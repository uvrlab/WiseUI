import socket
import json
from queue import Empty, Queue

import cv2
import numpy as np
import time
import struct

from SocketServer.DataPackage import DataFormat, DataType, HoloLens2PVImageData


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


def ReceiveLoop(sock, queue_data_received):
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


def DecodingLoop(queue_data_received:Queue, quit_event):
    while True:
        try:
            recvData = queue_data_received.get()
            queue_data_received.task_done()
            # queue get을 block하지 않고 timeout을 지정하면 쓰레드간의 병목 현상이 커져서 block했다..
            # 따라서 queue를 이용해서 쓰레드를 동기화할 때는 queue안에 메세지를 넣어서 thread를 빠져나오도록 구현했다.
            if recvData == b"#Disconnect#":
                break

            header_size = struct.unpack("<i", recvData[0:4])[0]
            bHeader = recvData[4:4 + header_size]
            header = json.loads(bHeader.decode())
            data_length = header['data_length']
            start_time = header['timestamp']
            image_data = recvData[4 + header_size: 4 + header_size + data_length]

            # print(header_size)
            # print(recvData[4:4 + header_size])
            # print(len(image_data))
            # print(data_length)
            #make_to_instance(header, image_data)
            time_to_process = (time.time() - start_time) + np.finfo(float).eps
            #print('Time to process data : {}, {} fps'.format(time_to_process, 1 / time_to_process))

        except Empty:
            # queue.get()을 block하지 않고 timeout을 지정한 상태에서, queue가 비면 Empty exception이 발생한다.
            # 하지만 현재는 block하지 않으므로 사실상 아무 기능을 하지 않는다.
            continue

def make_to_instance(header, data):
    dataType = header['dataType']
    timestamp = header['timestamp']

    if dataType == DataType.PV:
        width = header['width']
        height = header['height']
        dataFormat = header['dataFormat']

        dim = GetDimension(dataFormat)
        img_np = np.frombuffer(data, np.uint8).reshape((height, width, dim))
        # delay_time = time.time() - timestamp
        # print(f'Time delay : {delay_time}, fps : {1 / (delay_time + np.finfo(float).eps)}')

        # cv2.imwrite(f"{save_folder}PV_{frameID}.png", img_np)
        # cv2.namedWindow("pvimage")

        #pv_image = HoloLens2PVImageData(header, img_np)

        cv2.imshow("pvimage", img_np)
        cv2.waitKey(1)
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


def GetDimension(dataFormat: DataFormat):
    if dataFormat == DataFormat.RGBA or dataFormat == DataFormat.BGRA \
            or dataFormat == DataFormat.ARGB or dataFormat == DataFormat.Float32:
        return 4
    elif dataFormat == DataFormat.RGB:
        return 3
    elif dataFormat == DataFormat.U16:
        return 2
    elif dataFormat == DataFormat.U8:
        return 1
    else:
        raise (Exception("Invalid DataFormat Error."))
