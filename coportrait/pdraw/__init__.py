from .config import *

import logging
import azure.functions as func
from azure.storage.blob import BlockBlobService
import azure.cognitiveservices.vision.face as cf
from msrest.authentication import CognitiveServicesCredentials
import time,io,datetime

import cv2
import numpy as np

no_images = 10
end_date = datetime.datetime(2021,1,1)

blob = BlockBlobService(account_name=storage_acct_name, account_key=storage_acct_key) 
cogface = cf.FaceClient(cognitive_endpoint,CognitiveServicesCredentials(cognitive_key))

target_triangle = np.float32([[130.0,120.0],[170.0,120.0],[150.0,160.0]])
size = 300

def affine_transform(img,attrs):
    mc_x = (attrs['mouth_left']['x']+attrs['mouth_right']['x'])/2.0
    mc_y = (attrs['mouth_left']['y'] + attrs['mouth_right']['y']) / 2.0
    tr = cv2.getAffineTransform(np.float32([(attrs['pupil_left']['x'],attrs['pupil_left']['y']),
                                            (attrs['pupil_right']['x'],attrs['pupil_right']['y']),
                                            (mc_x,mc_y)]), target_triangle)                                
    return cv2.warpAffine(img,tr,(size,size))

def imdecode(bts):
    nparr = np.fromstring(bts, np.uint8)
    return cv2.imdecode(nparr,cv2.IMREAD_COLOR)


def main(req: func.HttpRequest) -> func.HttpResponse:
    logging.info('Python HTTP trigger function processed a request.')

    body = req.get_body()

    sec_p = int((end_date-datetime.datetime.now()).total_seconds())

    name =  f"{sec_p:09d}-"+time.strftime('%Y%m%d-%H%M%S')

    blob.create_blob_from_bytes("cin",name,body)

    img = imdecode(body)

    imgs = []

    res = cogface.face.detect_with_stream(io.BytesIO(body),return_face_landmarks=True)
    if res is not None and len(res)>0:
        tr = affine_transform(img,res[0].face_landmarks.as_dict())
        imgs.append(tr)
        res, body = cv2.imencode('.jpg',tr)
        if res:
            blob.create_blob_from_bytes("cmapped",name,body.tobytes())

    generator = blob.list_blobs("cmapped")
    n=no_images
    for x in generator:
        logging.info(x.name)
        cnt = blob.get_blob_to_bytes("cmapped",x.name)
        imgs.append(imdecode(cnt.content))
        n-=1
        if n==0:
            break

    imgs = np.array(imgs).astype(np.float32)/255.0
    res = np.average(imgs,axis=0)
    res = (res*255.0).astype(np.uint8)
    b = cv2.imencode('.jpg',res)[1]
    r = blob.create_blob_from_bytes("out",f"{name}.jpg",b.tobytes())
    logging.info(f"Created {name}.jpg")
    return func.HttpResponse(f"https://{storage_acct_name}.blob.core.windows.net/out/{name}.jpg")
