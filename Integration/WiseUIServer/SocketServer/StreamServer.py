import sys
import socket
import threading
from queue import Queue, Empty
import os
import logging

from SocketServer.DataPackage import DataType, ImageFormat
from SocketServer.static_functions import ReceiveLoop, DecodingLoop

logger = logging.getLogger(__name__)


class ClientObject:
    def __init__(self, socket, address):
        self.socket = socket
        self.address = address
        self.queue_data_receive = Queue()
        self.thread_receive = None
        self.thread_decode = None
        self.thread_process = None

        self.latest_frame_data = None
        self.queue_frame_data= Queue()
        self.quit_event = threading.Event()


    def GetLatestFrameData(self):
        return self.latest_frame_data
    def GetNextFrameData(self):
        try:
            data = self.queue_frame_data.get()
            self.queue_frame_data.task_done()
        except Empty:
            data = None

        return data
    def StartListeningClient(self, ProcessCallBack, DisconnectCallbackFunc):
        thread_start = threading.Thread(target=self.Listening, args=(ProcessCallBack, DisconnectCallbackFunc))
        thread_start.start()

    def Listening(self, ProcessCallBackFunc, DisconnectCallbackFunc):
        self.thread_receive = threading.Thread(target=ReceiveLoop, args=(self.socket, self.queue_data_receive,))
        self.thread_decode = threading.Thread(target=DecodingLoop, args=(self.queue_data_receive,self.quit_event,))
        self.thread_process = threading.Thread(target=ProcessCallBackFunc, args=(self.quit_event,))

        self.thread_receive.daemon = True
        self.thread_decode.daemon = True
        self.thread_process.daemon = True

        self.thread_receive.start()
        self.thread_decode.start()
        self.thread_process.start()

        self.thread_receive.join()
        self.quit_event.set()
        self.thread_decode.join()
        self.thread_process.join()


        DisconnectCallbackFunc(self)


class StreamServer:
    def __init__(self):
        self.save_folder = 'data/'
        self.list_client = []

    def Listening(self, serverHost, serverPort, ProcessCallBack):
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
                clientObject.StartListeningClient(ProcessCallBack, self.DisconnectCallback)
                print("current clients : {}".format(len(self.list_client)))

            except KeyboardInterrupt as e:
                sys.exit(0)

            except Exception:
                pass

    def DisconnectCallback(self, clientObj):
        print('Disconnected with ' + clientObj.address[0] + ':' + str(clientObj.address[1]))
        self.list_client.remove(clientObj)
