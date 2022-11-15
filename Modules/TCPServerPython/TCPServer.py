import socket
import struct
import sys
import os
import numpy as np
import cv2
import time
import open3d as o3d
import pickle as pkl

def tcp_server():
    serverHost = '' # localhost
    serverPort = 9090
    MAX_BUFFER_SIZE = 6+4*3+8+512*384*4
    save_folder = 'data/'

    if not os.path.isdir(save_folder):
        os.mkdir(save_folder)

    # Create a socket
    sSock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    # Bind server to port
    try:
        sSock.bind((serverHost, serverPort))
        print('Server bind to port '+str(serverPort))
    except socket.error as msg:
        print('Bind failed. Error Code : ' + str(msg[0]) + ' Message ' + msg[1])
        return

    sSock.listen(10)
    print('Start listening...')
    sSock.settimeout(3.0)
    while True:
        try:
            conn, addr = sSock.accept() # Blocking, wait for incoming connection
            break
        except KeyboardInterrupt:
            sys.exit(0)
        except Exception:
            continue

    print('Connected with ' + addr[0] + ':' + str(addr[1]))

    while True:
        # Receiving from client
        #try:
        #data = conn.recv(512*512*4+100)
        data = conn.recv(MAX_BUFFER_SIZE)
        
        if len(data)==0:
            continue
        
        frameID = struct.unpack("<i", data[0:4])[0]
        header = data[4:6].decode('utf-8')
        print('--------------------------\nHeader: ' + header)
        print(len(data))
    
        width = struct.unpack("<i", data[6:10])[0]
        height = struct.unpack("<i", data[10:14])[0]
        dim = struct.unpack("<i", data[14:18])[0]
        idx_img = 6+4*3
        
        time_send = struct.unpack("<d", data[idx_img+width*height*dim:idx_img+width*height*dim+8])[0]
        #time_send = 0
        
        
        print('frameID : %d'%frameID)
        print('width : %d '%width)
        print('height : %d'%height)
        print('dim : %d'%dim)
        print('time_send : ' + str(time_send))
        
        
        #print('compression :'+ str(comp))
                    
        if header == 'pv':                
            img_np = np.frombuffer(data[idx_img:idx_img+width*height*dim], np.uint8).reshape((height,width, dim))    
            timestamp = time.time()
            print('delay : '+ str(time.time() -time_send))
            
            #cv2.imwrite(f"{save_folder}{header}_{frameID}.png", img_np)
            print('Image with ts ' + str(timestamp) + ' is saved')
            
             
        if header == 'ds' or header == 'ab':
            img_np = np.frombuffer(data[idx_img:idx_img+width*height*dim], np.uint16).reshape((height,width))    
            #cv2.imwrite(f"{save_folder}{header}_{frameID}.tiff", img_np)
            print('Image with ts ' + str(timestamp) + ' is saved')
        
        
        if header == 's':
            # save depth sensor images
            data_length = struct.unpack(">i", data[1:5])[0]
            N = data_length
            depth_img_np = np.frombuffer(data[5:5+N], np.uint16).reshape((512,512))
            ab_img_np = np.frombuffer(data[5+N:5+2*N], np.uint16).reshape((512,512))
            timestamp = str(float(time.time()))
            cv2.imwrite(save_folder + timestamp+'_depth.tiff', depth_img_np)
            cv2.imwrite(save_folder + timestamp+'_abImage.tiff', ab_img_np)
            print('Image with ts ' + timestamp + ' is saved')
            
            
        if header == 'f':
            # save spatial camera images
            data_length = struct.unpack(">i", data[1:5])[0]
            ts_left, ts_right = struct.unpack(">qq", data[5:21])
    
            N = int(data_length/2)
            LF_img_np = np.frombuffer(data[21:21+N], np.uint8).reshape((480,640))
            RF_img_np = np.frombuffer(data[21+N:21+2*N], np.uint8).reshape((480,640))
            cv2.imwrite(save_folder + str(ts_left)+'_LF.tiff', LF_img_np)
            cv2.imwrite(save_folder + str(ts_right)+'_RF.tiff', RF_img_np)
            print('Image with ts %d and %d is saved' % (ts_left, ts_right))
            
            
        if header == 'p':
            # save point cloud
            N_pointcloud = struct.unpack(">i", data[1:5])[0]
            print("Length of point cloud:" + str(N_pointcloud))
            pointcloud_np = np.frombuffer(data[5:5+N_pointcloud*3*4], np.float32).reshape((-1,3))
            
            timestamp = str(float(time.time()))
            temp_filename_pc = timestamp + '_pc.ply'
            print(pointcloud_np.shape)
            o3d_pc = o3d.geometry.PointCloud()
            o3d_pc.points = o3d.utility.Vector3dVector(pointcloud_np.astype(np.float64))
            o3d.io.write_point_cloud(save_folder + temp_filename_pc, o3d_pc, write_ascii=True)
            print('Saved  image to ' + temp_filename_pc)

        '''
        except Exception as e:
            print('Exception occur : {}'.format(e))
            break
        '''  
            
    
    print('Closing socket...')
    sSock.close()


if __name__ == "__main__":
    tcp_server()
