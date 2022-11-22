import json
import time
from DataPackage import DataType, ObjectDataPackage
from StreamServer import StreamServer
from handtracker.module_SARTE import HandTracker
# import track_object
import cv2


track_hand = HandTracker()

def ReceiveCallBack(frame_info, rgb_image, client_socket):
    # intrinsic = frame_info['intrinsic'] # is not implemented yet.
    # extrinsic = frame_info['extrinsic'] # is not implemented yet.

    cv2.imshow("pv", rgb_image)
    cv2.waitKey(1)
    result_object = None  # track_object.Process(rgb_image)
    result_hand = track_hand.Process(rgb_image)

    """ Packing data for sending to hololens """
    resultData = dict()
    resultData['frameInfo'] = EncodeFrameInfo(frame_info)
    resultData['objectDataPackage'] = EndcodeObjectDataPackage(result_object)
    resultData['handDataPackage'] = EncodeHandDataPackage(result_hand)

    """ Send data """
    resultBytes = json.dumps(resultData).encode('utf-8')
    print(resultBytes)
    print("bytes of result : {}".format(len(resultBytes)))
    client_socket.send(resultBytes)

def EncodeFrameInfo(frame_info):
    frameInfo = dict()
    frameInfo['frameID'] = frame_info['frameID']
    frameInfo['timestamp_t1'] = frame_info['timestamp']
    frameInfo['timestamp_t2'] = float(time.time())
    return frameInfo

def EncodeHandDataPackage(hand_result):
    """ Encode hand data to json format """

    """ Example """
    handDataPackage = dict()
    num_joints = 21
    joints = list()
    for id in range(num_joints):
        joint = dict()
        joint['id'] = id
        joint['x'] = float(hand_result[id, 0])
        joint['y'] = float(hand_result[id, 1])
        joint['z'] = float(hand_result[id, 2])
        joints.append(joint)
    handDataPackage['joints'] = joints

    return handDataPackage

def EndcodeObjectDataPackage(object_result):
    """ Example """
    num_obj = 3
    #objectDataPackage = ObjectDataPackage()
    objectDataPackage = dict()

    objects = list()
    for obj_id in range(num_obj):
        objectInfo = dict()
        keyPoints = list()
        for kpt_id in range(8):
            keyPoint = dict()
            keyPoint['id'] = kpt_id
            keyPoint['x'] = 0.123
            keyPoint['y'] = 0.456
            keyPoint['z'] = 0.789
            keyPoints.append(keyPoint)

        objectInfo['keypoints'] = keyPoints
        objectInfo['id'] = obj_id
        objects.append(objectInfo)

    objectDataPackage['objects'] = objects

    return objectDataPackage

if __name__ == '__main__':

    server = StreamServer()
    server.Listening('', 9091, ReceiveCallBack)


