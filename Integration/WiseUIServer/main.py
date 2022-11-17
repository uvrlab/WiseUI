from StreamServer import StreamServer
# import track_hand
# import track_object
import cv2

def ReceieveCallBack(header, im_input):
    frameID = header['frameID']
    dataType = header['dataType']
    timestamp = header['timestamp']
    # intrinsic = header['intrinsic'] # is not implemented yet.
    # extrinsic = header['extrinsic'] # is not implemented yet.

    cv2.imshow("pv", im_input)
    cv2.waitKey(1)

    # result_hand = track_hand.Process(im_input)
    # result_object = track_object.Process(im_input)

    ''' Packing data for sending to hololens '''
    result_hand = dict()
    data_pack = PackingData(frameID, result_hand)

    ''' Send data'''
    #sock.send(data_pack)


def PackingData(frameID, result):
    dummy = dict()

    dummy['frameID'] = frameID
    return dummy

if __name__ == '__main__':

    server = StreamServer()
    server.Listening('', 9091, ReceieveCallBack)


