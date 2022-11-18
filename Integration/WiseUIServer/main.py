import json
import time
from DataPackage import DataType
from StreamServer import StreamServer
# import track_hand
# import track_object
import cv2

def ReceiveCallBack(frame_info, rgb_image, client_socket):
    # intrinsic = frame_info['intrinsic'] # is not implemented yet.
    # extrinsic = frame_info['extrinsic'] # is not implemented yet.

    # cv2.imshow("pv", rgb_image)
    # cv2.waitKey(1)
    result_object = None  # track_object.Process(im_input)
    result_hand = None # track_hand.Process(im_input)

    """ Packing data for sending to hololens """
    resultData = dict()
    resultData['frameID'] = frame_info['frameID']
    resultData['timestamp_receive'] = frame_info['timestamp']
    resultData['timestamp_send'] = time.time()
    resultData['objectData'] = EndcodeObjectData(result_object)
    resultData['handData'] = EncodeHandData(result_hand)

    """ Send data """
    resultBytes = json.dumps(resultData).encode('utf-8')
    print("bytes of result : {}".format(len(resultBytes)))
    client_socket.send(resultBytes)

def EncodeHandData(hand_result):
    """ Encode hand data to json format """

    """ Example """
    result_hand = dict()
    result_hand['numJoints'] = 27
    result_hand['joints'] = [[0.123] * 3 ] * 27

    return result_hand

def EndcodeObjectData(object_result):
    result_Object = dict()
    result_Object['numObjects'] = 3
    # result_Object['bboxs'] = [[0.0] * 4] * 3
    # result_Object['labels'] = [0] * 3
    # result_Object['scores'] = [0.0] * 3

    return result_Object

if __name__ == '__main__':

    server = StreamServer()
    server.Listening('', 9091, ReceiveCallBack)


