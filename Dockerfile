FROM microsoft/azure-functions-runtime:v2.0.0-beta1
ARG STORAGE_ACCOUNT

ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureWebJobsStorage=$STORAGE_ACCOUNT 
COPY . /home/site/wwwroot 