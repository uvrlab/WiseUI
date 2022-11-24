import sys
import socket
import threading
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
        self.queue_pv_frame = Queue()
        self.queue_depth_frame = Queue()
        self.queue_pc_frame = Queue()



    def get_latest_pv_frame(self):
        return self.latest_pv_image
    def get_next_pv_frame(self):
        try:
            data = self.queue_frame_data.get()
            self.queue_frame_data.task_done()
        except Empty:
            raise Empty

        return data
    def start_listening(self, processing_loop, disconnect_callback):
        thread_start = threading.Thread(target=self.listening, args=(processing_loop, disconnect_callback))
        thread_start.start()

    def listening(self, processing_loop, disconnect_callback):
        self.thread_receive = threading.Thread(target=receive_loop, args=(self.socket, self.queue_received_data,))
        self.thread_depackage = threading.Thread(target=depackage_loop, args=(self.queue_received_data,))
        self.thread_process = threading.Thread(target=processing_loop, args=(self.quit_event,))

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
