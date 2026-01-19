#!/usr/bin/env bash
dotnet publish -c Release --self-contained true -p:PublishSingleFile=true
