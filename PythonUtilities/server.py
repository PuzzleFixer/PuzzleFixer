import array
import zmq
import tsne



if __name__ == "__main__":
    context = zmq.Context()
    socket = context.socket(zmq.REP)
    socket.bind("tcp://*:15555")

    while True:
        # bytesData = socket.recv()
        message = socket.recv_multipart()
        
        if (message[0].decode('utf-8') == "tSNECluster"):
            
            num = int(message[1])
            arr = array.array('f', message[2]).tolist()
            
            embeddedClusterID = tsne.ClusteringPointCloud(arr, num)
            socket.send(embeddedClusterID)

        
