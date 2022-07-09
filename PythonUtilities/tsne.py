from sklearn.manifold import TSNE
import numpy as np
import matplotlib.pyplot as plt
import torch
from torch import FloatTensor
from chamferdist import ChamferDistance
from sklearn.cluster import DBSCAN


candidateArr = np.zeros(0)
round = 0

# https://github.com/krrish94/chamferdist
def GetNearestCharmferDis(ns):
    if candidateArr.shape[0] > 0:
        source_clouds = candidateArr
        target_cloud = torch.FloatTensor(np.reshape(ns, (1, int(len(ns) / 3), 3))).cuda()
        chamferDist = ChamferDistance()
        dist = []
        for source_cloud in source_clouds:
            s = torch.FloatTensor(np.reshape(source_cloud, (1, int(len(source_cloud) / 3), 3))).cuda()
            t = target_cloud
            dist.append(chamferDist(s, t, bidirectional=True).detach().cuda().item())
        
        idx = dist.index(min(dist))
        return np.array([idx, idx])
    else:
        return np.zeros(0)


def GetNearestNodeByNode(ns):
    if candidateArr.shape[0] > 0:
        targetSkeleton = np.asarray(ns)
        t = np.reshape(targetSkeleton, (1, int(targetSkeleton.shape[0] / 3), 3))

        x = np.reshape(candidateArr, (candidateArr.shape[0], int(candidateArr.shape[1] / 3), 3))
        removeIdx = np.unique((np.transpose(np.asarray(np.where(t == np.ones((1, 3)) * 100))))[:, 1])
        matchedCandidate = []
        for i in range(len(x)):
            matchedCandidate.append(np.delete(x[i], removeIdx, axis=0))
        t = np.delete(t[0], removeIdx, axis=0)

        minDis = float('inf')
        minIdx = 0
        disCache = []
        for i in range(len(matchedCandidate)):
            s = matchedCandidate[i]
            tempDis = np.average(np.sum(np.square(t - s), axis=1))
            disCache.append(tempDis)
            if tempDis < minDis:
                minDis = tempDis
                minIdx = i
        return np.array([minIdx, minIdx])
    else:
        return np.zeros(0)

# tSNE first, clustering next
def ClusteringPointCloud(x, n):
    global round
    if round == 0:
        curEps = 0.05
        curSampleNim = 3
        pp = 5
    elif round == 1:
        curEps = 0.1
        curSampleNim = 4
        pp = 5
    else:
        curEps = 0.1
        curSampleNim = 4
        pp = 5
    round += 1

    disMatrix = GetCharmferDis(x, n)

    while True:
        x_embedded = GetEmbedTSNE(disMatrix, pp, False)

        # normalize x_embedded
        minx = min(x_embedded[:, 0])
        miny = min(x_embedded[:, 1])
        maxx = max(x_embedded[:, 0])
        maxy = max(x_embedded[:, 1])
        x_embedded_norm = x_embedded
        x_embedded_norm[:, 0] = (x_embedded_norm[:, 0] - minx) / (maxx - minx)
        x_embedded_norm[:, 1] = (x_embedded_norm[:, 1] - miny) / (maxy - miny)

        db = DBSCAN(eps=curEps, min_samples=curSampleNim).fit(x_embedded_norm)
        x_labels = db.labels_
        
        
        if max(x_labels) < 0:
            x_labels = np.zeros(len(x_labels))
        else:
            clusterIdx = []
            for i in range(len(x_labels)):
                if x_labels[i] >= 0:
                    clusterIdx.append(i)

            while min(x_labels) < 0:
                noiseIdx = []
                for i in range(len(x_labels)):
                    if x_labels[i] < 0:
                        noiseIdx.append(i)
                
                minDist = []
                minIdx = []
                for i in clusterIdx:
                    noiseDis = disMatrix[i][noiseIdx]
                    idx = np.argmin(noiseDis)
                    minDist.append(noiseDis[idx])
                    minIdx.append([i, noiseIdx[idx]])
                neighbor = minIdx[np.argmin(minDist)]
                x_labels[neighbor[1]] = x_labels[neighbor[0]]

        print("clustering finished")

        # show result
        plt.figure()
        unique_labels = set(x_labels)
        colors = [plt.cm.Spectral(each) for each in np.linspace(0, 1, len(unique_labels))]
        core_samples_mask = np.zeros_like(db.labels_, dtype=bool)
        core_samples_mask[db.core_sample_indices_] = True
        for k, col in zip(unique_labels, colors):
            if k == -1: 
                col = [0, 0, 0, 1]

            class_member_mask = (x_labels == k)

            xy = x_embedded_norm[class_member_mask & core_samples_mask]
            plt.plot(xy[:, 0], xy[:, 1], 'o', markerfacecolor=tuple(col),
                markeredgecolor='k', markersize=14)

            xy = x_embedded_norm[class_member_mask & ~core_samples_mask]
            plt.plot(xy[:, 0], xy[:, 1], 'o', markerfacecolor=tuple(col),
                markeredgecolor='k', markersize=6)
        name = "%.2f_%d_%d_Estimated number of clusters-%d all points-%d" %(curEps, curSampleNim, pp, len(unique_labels), len(x_labels))

        break

    # [embedPosition2D clusterID]
    return np.column_stack((np.array(x_embedded_norm),np.array(x_labels, dtype='float32')))



def CharmferDis(x):
    n = len(x)
    x_tensor = []
    for i in range(n):
        x_tensor.append(FloatTensor(np.reshape(x[i], (1, x[i].shape[0], x[i].shape[1]))).cuda())
    disMatrix = np.zeros((n, n))
    chamferDist = ChamferDistance()
    for i in range(n):
        for j in range(i + 1, n):
            disMatrix[i, j] = chamferDist(x_tensor[i], x_tensor[j], bidirectional=True)
            disMatrix[j, i] = disMatrix[i, j]

    return disMatrix


def RemoveNoneCandidatePoints(x):
    xx = np.reshape(x, (x.shape[0], int(x.shape[1] / 3), 3))
    removeIdx = np.transpose(np.asarray(np.where(xx == np.ones((1, 3)) * 100)))
    candidatePointCloud = []
    for i in range(len(xx)):
        candidatePointCloud.append(np.delete(xx[i], np.unique(removeIdx[removeIdx[:, 0] == i][:, 1]), axis=0))

    return candidatePointCloud

def ScalePointCloud(x):
    xPointFlat = []
    for i in range(len(x)):
        for j in range(len(x[i])):
            xPointFlat.append(x[i][j])
    xPointFlat = np.array(xPointFlat)
    
    for dim in range(3):
        minPos = min(xPointFlat[:, dim])
        maxPos = max(xPointFlat[:, dim])
        for i in range(len(x)):
            x[i][:, dim] = (x[i][:, dim]  - minPos) / (maxPos - minPos)

    return x


def DimensionReduction(x, n):
    global candidateArr
    x = np.reshape(x, (n, int(len(x) / n)))

    candidateArr = x

    x_embedded = TSNE(n_components=2, perplexity=10).fit_transform(x)

    return x_embedded


def GetCharmferDis(x, n):
    global candidateArr
    x = np.reshape(x, (n, int(len(x) / n)))
    candidateArr = x

    candidatePointCloud = RemoveNoneCandidatePoints(x)

    candidatePointCloud = ScalePointCloud(candidatePointCloud)

    disMatrix = CharmferDis(candidatePointCloud)

    return disMatrix


def GetEmbedTSNE(disMatrix, pp, show=True):
    x_embedded = TSNE(n_components=2, perplexity=pp, early_exaggeration=20, metric="precomputed", random_state=4231, square_distances=True).fit_transform(disMatrix)
    
    if (show):
        plt.scatter(x_embedded[:, 0], x_embedded[:, 1], alpha=1)
        plt.show()

    return x_embedded
