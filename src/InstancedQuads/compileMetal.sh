#!/bin/bash

# executed from .sln space
cd $1
for metal_shader in *.metal 
do
    fname=${metal_shader%.*}
    length=${#fname}
    if [ "$length" -le 1 ]
    then
        echo "No Metal Shaders detected"
        break
    fi
    echo "Compiling $metal_shader"
    xcrun -sdk macosx metal "$metal_shader" -o "$fname.air"
    xcrun -sdk macosx metallib "$fname.air" -o "$fname.metallib"
    rm "$fname.air"
done