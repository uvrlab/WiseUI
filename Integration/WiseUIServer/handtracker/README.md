conda create -n wiseui python=3.7
conda install pytorch==1.8.0 torchvision==0.9.0 torchaudio==0.8.0 cudatoolkit=11.1 -c pytorch -c conda-forge

pip install -r requirements.txt

# for detectron2 
python -m pip install detectron2 -f \
  https://dl.fbaipublicfiles.com/detectron2/wheels/cu111/torch1.8/index.html
  
pip install opencv-contrib-python 
pip install easydict
pip install pyzmq
