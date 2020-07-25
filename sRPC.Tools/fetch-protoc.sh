#!/bin/sh

rm -r protoc-*      

curl https://api.github.com/repos/protocolbuffers/protobuf/releases/latest 2> /dev/null \
    | grep browser_download_url \
    | grep -o "http.*protoc-.*zip" \
    | while IFS= read -r file; do
        name=$(echo $file | grep -o "protoc-.*zip" | sed 's/-[0-9\.]*-/-/g')
        dir=$(basename $name .zip)
        echo download $file to $name
        wget "$file" -O $name
        echo unzip $name in $dir
        unzip $name bin* -d "${dir}/" -o
        rm $name
    done;
