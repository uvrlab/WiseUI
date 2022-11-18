import json
import time
from DataPackage import DataType
from StreamServer import StreamServer
# import track_hand
# import track_object
import cv2

def ReceiveCallBack(header, im_input, socket):
    frameID = header['frameID']

    # intrinsic = header['intrinsic'] # is not implemented yet.
    # extrinsic = header['extrinsic'] # is not implemented yet.

    # cv2.imshow("pv", im_input)
    # cv2.waitKey(1)

    # result_hand = track_hand.Process(im_input)
    # result_object = track_object.Process(im_input)

    ''' Packing data for sending to hololens '''

    #data_pack = PackingData(frameID, result_hand)


    ''' Send data'''

    #data_pack = frameID.to_bytes(4, byteorder='little')
    #socket.send(data_pack)

    resultDataPack = dict()
    resultDataPack['frameID'] = frameID
    resultDataPack['timestamp_receive'] = header['timestamp']
    resultDataPack['timestamp_send'] = time.time()
    resultDataPack['objectData'] = SampleObjectData()
    resultDataPack['handData'] = SampleHandData()

    resultBytes = json.dumps(resultDataPack).encode('utf-8')
    print(len(resultBytes))
    socket.send(resultBytes)

    #queue_data_to_send.put(data_pack)
    #queue_data_to_send.join()


def SampleHandData():
    result_hand = dict()
    result_hand['numJoints'] = 27
    result_hand['joints'] = [[1.2] * 3 ] * 27

    return result_hand

def SampleObjectData():
    result_Object = dict()
    result_Object['numObjects'] = 3
    # result_Object['bboxs'] = [[0.0] * 4] * 3
    # result_Object['labels'] = [0] * 3
    # result_Object['scores'] = [0.0] * 3

    return result_Object

if __name__ == '__main__':

    server = StreamServer()
    server.Listening('', 9091, ReceiveCallBack)


