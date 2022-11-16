import sys
import socket
import json
import threading
from queue import Queue
from datetime import datetime
import csv
from ast import literal_eval
import time
import numpy as np
import cv2
import time
# import open3d as o3d
import pickle as pkl
import os
import struct

save_folder = 'data/'

serverHost = ''  # localhost
serverPort = 9091


class DataType:
    PV = 1
    Depth = 2
    PC = 3
    Sensor = 4


class ImageCompression:
    None_ = 0
    JPEG = 1
    PNG = 2

class ImageFormat:
    INVALID = -1
    RGBA = 1
    BGRA = 2 # proper to numpy format.
    ARGB = 3
    RGB = 4
    U16 = 5
    U8 = 6
    Float32 = 7


def ReceiveLoop(sock, queue_data):
    while True:
        try:
            start_time = time.time()
            recvData = recv_msg(sock)
            eps = 0.0000001
            time_to_receive = time.time() - start_time
            print('Time to receive data : {}, {} fps'.format(time_to_receive, 1/(time_to_receive + eps)))

            queue_data.put(recvData)
            queue_data.join()

            if not recvData:
                break
            # print('Data received from' + str(addr) + ' : ' + str(datetime.now()))

        except socket.error as msg:
            print('Error Code : ' + str(msg[0]) + ' Message ' + msg[1])
            break
            # continue


def ProcessingLoop(queue_data):
    while True:
        start_time = time.time()

        recvData = queue_data.get()
        queue_data.task_done()

        if not recvData:
            break

        header_size = struct.unpack("<i", recvData[0:4])[0]
        bHeader = recvData[4:4 + header_size]
        header = json.loads(bHeader.decode())
        data_length = header['data_length']

        image_data = recvData[4 + header_size: 4 + header_size + data_length]

        # print(header_size)
        print(recvData[4:4 + header_size])
        #print(len(image_data))
        #print(data_length)
        ProcessingData(header, image_data)
        time_to_process = time.time() - start_time
        print('Time to process data : {}, {} fps'.format(time_to_process, 1 / time_to_process))


def ProcessingData(header, data):
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

        dim = getDimension(imageFormat)

        # if img_compression == ImageCompression.JPEG:
        # encode_param=[int(cv2.IMWRITE_JPEG_QUALITY), jpgQuality]
        # data = cv2.imdecode(data, encode_param)

        img_np = np.frombuffer(data, np.uint8).reshape((height, width, dim))
        delay_time = time.time() - timestamp
        eps = 0.0000001
        print(f'Time delay : {delay_time}, fps : {1 / (delay_time + eps)}')

        #cv2.imwrite(f"{save_folder}PV_{frameID}.png", img_np)
        cv2.namedWindow("pvimage")
        cv2.imshow("pvimage", img_np)
        cv2.waitKey(1)
        # print('Image with ts ' + str(timestamp) + ' is saved')


def recv_msg(sock):
    # Read message length and unpack it into an integer
    raw_msglen = recvall(sock, 4)
    if not raw_msglen:
        return None
    #msglen = struct.unpack('<i', raw_msglen)[0]
    msglen = int.from_bytes(raw_msglen, "little")
    # Read the message data
    return recvall(sock, msglen)


def recvall(sock, n):
    # Helper function to recv n bytes or return None if EOF is hit
    data = bytearray()
    while len(data) < n:
        packet = sock.recv(n - len(data))
        if not packet:
            return None
        data.extend(packet)
    return data

def getDimension(imgFormat : ImageFormat):
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
        raise(Exception("Invalid ImageFormat Error."))

if __name__ == '__main__':
    if not os.path.isdir(save_folder):
        os.mkdir(save_folder)

    while(True):

        queue_data = Queue()
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

        thread_receive = threading.Thread(target=ReceiveLoop, args=(sock, queue_data,))
        thread_process = threading.Thread(target=ProcessingLoop, args=(queue_data,))

        thread_receive.daemon = True
        thread_process.daemon = True

        thread_receive.start()
        thread_process.start()

        thread_receive.join()
        thread_process.join()

        print('Disconnected with ' + addr[0] + ':' + str(addr[1]))