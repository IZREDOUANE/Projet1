from typing import List
from urllib import request, error

import sys

import json
from types import SimpleNamespace

URL_LIST = 'http://localhost:59154/api/intranet/view/reprise_documents/list'
URL_DOWNLOAD = 'http://localhost:59154/api/intranet/view/reprise_documents/download'
URL_UPLOAD = 'http://localhost:59154/api/ged/upload/bulk'

BULK_SIZE_FOR_UPLOAD = 100

class Document:
    def __init__(self, fileName: str, fileExtension: str, fileCategory: str, modifiedBy: str):
        self.FileName = fileName
        self.FileExtension = fileExtension
        self.FileCategory = fileCategory
        self.ModifiedBy = modifiedBy

class DocumentPayload:
    def __init__(self, authorizedUsersSfId: str, details: Document, content: str):
        self.IsPublic = False
        self.AuthorizedUsersSfId = [ authorizedUsersSfId ]
        self.Details = details
        self.Content = content


def GetListOfDocuments():
    '''
    get the list of documents available in Intranet
    '''
    
    try:
        rqst = request.urlopen(URL_LIST)
        raw_data = rqst.read()
        encoding = rqst.info().get_content_charset('utf-8')

        return (rqst.getcode(), json.loads(raw_data.decode(encoding), object_hook=lambda d: SimpleNamespace(**d)))

    except error.HTTPError as e:
        return (e.code, 'error %d: %s'%(e.code, e.reason))

def GetDocumentContent(path):
    '''
    Gets the content of a document from path from intranet

    Keyword argument:
    path -- absolute path of the document
    '''

    data = {'Path': path}

    rqst = request.Request(URL_DOWNLOAD, json.dumps(data).encode('utf-8'))
    rqst.add_header('Content-Type', 'application/json; charset=utf-8')

    try:
        resp = request.urlopen(rqst)

        raw_data = resp.read()
        encoding = resp.info().get_content_charset('utf-8')

        return (resp.getcode(), json.loads(raw_data.decode(encoding), object_hook=lambda d: SimpleNamespace(**d)))
        

    except error.HTTPError as e:
        return (e.code, 'error %d: %s'%(e.code, e.reason))

def CreateNewDocumentDTOObject(doc, content: str):
    '''
    Creates a new payload DTO for uploading purposes

    Keyword arguments:
    doc -- infos and details of the doc
    content -- base 64 string of the binary of the doc
    '''

    details = Document(doc.doc_Name, 'pdf', doc.doc_DocumentType, 'Reprise Documentaire WebRunJob')
    payload = DocumentPayload(doc.doc_Owner_SFID, details, content)

    return payload

def obj_dict(obj):
    '''
    Parses a list of objects to a dictionary
    '''
    return obj.__dict__

def UploadDocumentPost(docsToUpload: List[DocumentPayload]):
    '''
    POST request the upload of the documents we want to store in the GED v3
    '''

    data = { 'Documents': docsToUpload }

    rqst = request.Request(URL_UPLOAD, json.dumps(data, default=obj_dict).encode('utf-8'))
    rqst.add_header('Content-Type', 'application/json; charset=utf-8')
    
    try:
        resp = request.urlopen(rqst)

        raw_data = resp.read()
        encoding = resp.info().get_content_charset('utf-8')

        return (resp.getcode(), json.loads(raw_data.decode(encoding), object_hook=lambda d: SimpleNamespace(**d)))
    
    except error.HTTPError as e:
        return (e.code, 'error %d: %s'%(e.code, e.reason))

def UploadDocument(errors: List[str], docsToUpload: List[DocumentPayload]):
    '''
    Uploads a bulk collection of documents in one request from Azure API

    Keyword arguments:
    errors -- error history of the all the uploads made by the script
    docsToUpload -- bulk collection to upload to the GED v3
    '''

    errorHist = []
    resp = UploadDocumentPost(docsToUpload)
            
    if (resp[0] >= 300):
        print(resp[1])

    elif len(resp[1].errorMessages) > 0:
       errorHist = errors + resp[1].errorMessages

    docsToUpload = []

    return (errorHist, [])

########### Main script launch #################

def main():
    listResponse = GetListOfDocuments()


    if ((listResponse[0]//100) != 2):
        print(listResponse[1])
        exit(0)

    documents = listResponse[1]

    docToUpload = []
    errors = []

    for doc in documents:
        path = doc.doc_FilePath
        docResp = GetDocumentContent(path)

        if (docResp[0]//100) == 2:
            docToUpload.append(CreateNewDocumentDTOObject(doc, docResp[1].content))

        if len(docToUpload) == BULK_SIZE_FOR_UPLOAD:
            (errors, docToUpload) = UploadDocument(errors, docToUpload)

    (errors, docToUpload) = UploadDocument(errors, docToUpload)

    print('\n'.join(errors))
    print(len(documents) - len(errors), ' documents inserted out of ', len(documents))
    print('script finished to process')
    sys.stdout.flush()

if __name__ == '__main__':
    main()