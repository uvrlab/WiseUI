import sys
import socket
import threading
from collections import deque
from queue import Queue, Empty
import os
import logging

from SocketServer.DataPackage import DataType, DataFormat
from SocketServer.static_functions import receive_loop, depackage_loop

logger = logging.getLogger(__name__)

class ClientObject:
    def __init__(self, socket, address):
        self.socket = socket
        self.address = address

        self.thread_receive = None
        self.thread_depackage = None
        self.thread_process = None
        self.quit_event = threading.Event()

        self.queue_received_data = Queue()
        self.queue_pv_frame = deque()
        self.queue_depth_frame = deque()
        self.queue_pc_frame = deque()

        self.latest_pv_image = None
        self.latest_depth_frame = None
        self.latest_pc_frame = None

        self.lock_pv_frame = threading.Lock()
        self.lock_depth_frame = threading.Lock()
        self.lock_pc_frame = threading.Lock()

        self.is_new_pv_frame = False
        self.is_new_depth_frame = False
        self.is_new_pc_frame = False

    def instert_pv_frame(self, frame):
        #self.lock_pv_frame.acquire()
        #self.latest_pv_image = frame
        self.queue_pv_frame.append(frame)
        #self.queue_pv_frame.join()
        #self.is_new_pv_frame = True
        #self.lock_pv_frame.release()

    def get_latest_pv_frame(self):
        try:
            #self.lock_pv_frame.acquire()
            #frame = self.latest_pv_image
            frame = self.queue_pv_frame.pop()
            #self.queue_pv_frame.task_done()
            #self.lock_pv_frame.release()
            #self.is_new_pv_frame = False
            return frame
        except Exception:
            raise Exception("No new frame")

    def get_oldest_pv_frame(self):
        try:
            #self.lock_pv_frame.acquire()
            frame = self.queue_pv_frame.popleft()
            return frame

        except Exception:
            raise Exception("No new frame")

        return copied_frame
    def start_listening(self, processing_loop, disconnect_callback):
        thread_start = threading.Thread(target=self.listening, args=(processing_loop, disconnect_callback))
        thread_start.start()

    def listening(self, processing_loop, disconnect_callback):
        self.thread_receive = threading.Thread(target=receive_loop, args=(self.socket, self.queue_received_data,))
        self.thread_depackage = threading.Thread(target=depackage_loop, args=(self.queue_received_data, self.instert_pv_frame))
        self.thread_process = threading.Thread(target=processing_loop, args=(self,))

        self.thread_receive.daemon = True
        self.thread_depackage.daemon = True
        self.thread_process.daemon = True

        self.thread_receive.start()
        self.thread_depackage.start()
        self.thread_process.start()

        self.thread_receive.join()
        self.thread_depackage.join()
        self.quit_event.set()
        self.thread_process.join()

        disconnect_callback(self)


class StreamServer:
    def __init__(self):
        self.save_folder = 'data/'
        self.list_client = []

    def listening(self, serverHost, serverPort, processing_loop):
        if not os.path.isdir(self.save_folder):
            os.mkdir(self.save_folder)

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
        # serverSock.settimeout(3.0)

        while True:
            try:
                sock, addr = serverSock.accept()  # Blocking, wait for incoming connection
                print('Connected with ' + addr[0] + ':' + str(addr[1]))

                clientObject = ClientObject(sock, addr)
                self.list_client.append(clientObject)
                clientObject.start_listening(processing_loop, self.disconnect_callback)
                print("current clients : {}".format(len(self.list_client)))

            except KeyboardInterrupt as e:
                sys.exit(0)

            except Exception:
                pass

    def disconnect_callback(self, clientObj):
        print('Disconnected with ' + clientObj.address[0] + ':' + str(clientObj.address[1]))
        self.list_client.remove(clientObj)
