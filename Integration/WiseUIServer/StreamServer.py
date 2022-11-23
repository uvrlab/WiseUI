import errno
import sys
import socket
import json
import threading
from functools import partial
from queue import Queue, Empty
from datetime import datetime
import csv
from ast import literal_eval
import time
from stat import S_ISSOCK

import numpy as np
import cv2
import time
# import open3d as o3d
import pickle as pkl
import os
import struct
import logging

from DataPackage import DataType, ImageFormat

logger = logging.getLogger(__name__)

event = threading.Event()


def SendLoop(sock, queue_data_to_send):
    while True:
        try:
            # if queue_data_to_send.empty():
            #     if event.is_set():
            #         print("SendLoop break")
            #         break
            #     else:
            #         continue
            try:
                data = queue_data_to_send.get()
                sock.send(data)
                queue_data_to_send.task_done()
            except Empty:
                continue
                # print("SendLoop break")
                # if event.is_set():
                # print("SendLoop break")
                # break

        except socket.error as msg:
            print('Error Code : ' + str(msg[0]) + ' Message ' + msg[1])
            break


def ReceiveLoop(sock, queue_data_received, queue_data_send):
    while True:
        try:
            start_time = time.time()
            # check socket is alive
            # is_socket_closed(sock)
            recvData = recv_msg(sock)  # buffer size를 고정하지 않고 첫 4 byte에 기록된 buffer size 만큼 이어서 받는다.
            # print(recvData)

            if recvData is None:
                continue

            queue_data_received.put(recvData)
            queue_data_received.join()

            if recvData == b"#Disconnect#":
                break
            time_to_receive = time.time() - start_time
            # print('Time to receive data : {}, {} fps'.format(time_to_receive, 1 / (time_to_receive + np.finfo(float).eps)))

            """ echo test 용 """
            # queue_data_send.put(recvData)
            # queue_data_send.join()

            # print('Data received from' + str(addr) + ' : ' + str(datetime.now()))

        except socket.error as msg:
            print('Error Code : ' + str(msg[0]) + ' Message ' + msg[1])
            break
            # continue


def DecodingLoop(socket, queue_data_received, queue_data_send, ProcessCallBack):
    while True:
        try:
            recvData = queue_data_received.get()
            queue_data_received.task_done()

            if recvData == b"#Disconnect#":
                break

            start_time = time.time()
            header_size = struct.unpack("<i", recvData[0:4])[0]
            bHeader = recvData[4:4 + header_size]
            header = json.loads(bHeader.decode())
            data_length = header['data_length']

            image_data = recvData[4 + header_size: 4 + header_size + data_length]

            # print(header_size)
            # print(recvData[4:4 + header_size])
            # print(len(image_data))
            # print(data_length)
            DecodingData(socket, header, image_data, ProcessCallBack, queue_data_send)
            time_to_process = time.time() - start_time
            # print('Time to process data : {}, {} fps'.format(time_to_process, 1 / time_to_process))

        except Empty:
            # queue_data_received.task_done()
            continue
            # print("DecodingLoop break")
            # if event.is_set():
            # print("DecodingLoop break")
            # break


def DecodingData(socket, header, data, ProcessCallBack, queue_data_send):
    dataType = header['dataType']
    data_length = header['data_length']
    timestamp = header['timestamp']
    frameID = header['frameID']
    img_compression = header['dataCompressionType']
    jpgQuality = header['imageQulaity']

    if dataType == DataType.PV:
        width = header['width']
        height = header['height']
        imageFormat = header['imageFormat']

        dim = GetDimension(imageFormat)

        # if img_compression == ImageCompression.JPEG:
        # encode_param=[int(cv2.IMWRITE_JPEG_QUALITY), jpgQuality]
        # data = cv2.imdecode(data, encode_param)

        img_np = np.frombuffer(data, np.uint8).reshape((height, width, dim))
        delay_time = time.time() - timestamp
        # print(f'Time delay : {delay_time}, fps : {1 / (delay_time + np.finfo(float).eps)}')

        ProcessCallBack(header, img_np, socket)
        # cv2.imwrite(f"{save_folder}PV_{frameID}.png", img_np)
        # cv2.namedWindow("pvimage")
        # cv2.imshow("pvimage", img_np)
        # cv2.waitKey(1)
        # print('Image with ts ' + str(timestamp) + ' is saved')


def recv_msg(sock):
    # Read message length and unpack it into an integer
    raw_msglen = recv_all(sock, 4)
    if not raw_msglen:
        return None
    # msglen = struct.unpack('<i', raw_msglen)[0]
    msglen = int.from_bytes(raw_msglen, "little")
    # Read the message data
    return recv_all(sock, msglen)


def recv_all(sock, n):
    # Helper function to recv n bytes or return None if EOF is hit
    data = bytearray()
    while len(data) < n:
        packet = sock.recv(n - len(data))
        if not packet:
            return None
        data.extend(packet)
    return data


def GetDimension(imgFormat: ImageFormat):
    if imgFormat == ImageFormat.RGBA or imgFormat == ImageFormat.BGRA \
            or imgFormat == ImageFormat.ARGB or imgFormat == ImageFormat.Float32:
        return 4
    elif imgFormat == ImageFormat.RGB:
        return 3
    elif imgFormat == ImageFormat.U16:
        return 2
    elif imgFormat == ImageFormat.U8:
        return 1
    else:
        raise (Exception("Invalid ImageFormat Error."))


class StreamServer:
    def __init__(self):
        self.save_folder = 'data/'

    def Listening(self, serverHost, serverPort, ProcessCallBack):
        if not os.path.isdir(self.save_folder):
            os.mkdir(self.save_folder)

        while (True):
            queue_data_received = Queue()
            queue_data_send = Queue()
            # Create a socket

            serverSock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
            # Bind server to port
            try:
                serverSock.bind((serverHost, serverPort))
                print('Server bind to port ' + str(serverPort))
            except socket.error as msg:
                print(msg)
                print('Bind failed. Error Code : Message ' + msg[0])
                # print(msg[0])
                # print(msg[1])
                sys.exit(0)

            serverSock.listen(10)
            print('Start listening...')
            serverSock.settimeout(3.0)

            while True:
                try:
                    sock, addr = serverSock.accept()  # Blocking, wait for incoming connection
                    break
                except KeyboardInterrupt:
                    sys.exit(0)

                except Exception:
                    continue

            print('Connected with ' + addr[0] + ':' + str(addr[1]))
            # ReceiveLoop(conn, queue)

            event.clear()

            thread_receive = threading.Thread(target=ReceiveLoop, args=(sock, queue_data_received, queue_data_send))
            # thread_send = threading.Thread(target=SendLoop, args=(sock, queue_data_send,))
            thread_decode = threading.Thread(target=DecodingLoop,
                                              args=(sock, queue_data_received, queue_data_send, ProcessCallBack))



            thread_receive.daemon = True
            # thread_send.daemon = True
            thread_decode.daemon = True

            thread_receive.start()
            # thread_send.start()
            thread_decode.start()

            thread_receive.join()
            # thread_send.join()
            thread_decode.join()

            print('Disconnected with ' + addr[0] + ':' + str(addr[1]))
