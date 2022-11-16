# import tcpip
# import track_hand
# import track_object

# main function.
if __name__ == '__main__':

    ''' Start to receive data'''
    # tcpip.ServerOpen(server, port)
    # tcpip.BeginReceive(ReceieveCallBack)

def ReceieveCallBack(im_input):
    # result_hand = track_hand.Process(im_input)
    # result_object = track_object.Process(im_input)

    ''' Packing data for sending to hololens '''
    # data_pack =  PackingData(result_hand, result_object)

    ''' Send data'''
    # tcpip.send(data_pack)



def PackingData(result_hand, result_object):
    #

