#!/bin/sh
script=$(readlink -f "$0")
basedir=$(dirname "${script}")

rm -r protoc-*      

curl https://api.github.com/repos/protocolbuffers/protobuf/releases/latest 2> /dev/null \
    | grep browser_download_url \
    | grep -o "http.*protoc-.*zip" \
    | while IFS= read -r file; do
        name=$(echo $file | grep -o "protoc-.*zip" | sed 's/-[0-9\.]*-/-/g')
        dir=$(basename $name .zip)
        echo download $file to $name
        wget "$file" -O "${basedir}/${name}"
        echo unzip "${basedir}/${name}" in "${basedir}/${dir}"
        unzip "${basedir}/${name}" bin* -d "${basedir}/${dir}/" -o
        rm "${basedir}/${name}"
    done;
