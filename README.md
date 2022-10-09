# WiseUI Integration 
ARRC-UVR Wise UI 통합 프로젝트

## Contents

- Application/
  - [HoloLens2StreamTCPTestUnity](https://github.com/IkbeomJeon/WiseUI/tree/master/Applications/HoloLens2StreamTCPTestUnity) : HoloLens2Stream Plugin을 이용하여 HoloLens2의 센서값들을  Visualization하고 TCP 서버에 전송하는 example이 담긴 유니티 프로젝트.

  
  
- Modules/

  - [ARRCObjectron](https://gitlab.com/IkbeomJeon/arrcobjectron) : 3D Object pose estimation 모듈.

  - [HoloLens2Stream](https://github.com/IkbeomJeon/HoloLens2Stream) :  HoloLens2 Stream에 접근하여 각종 센서 값을 받아오는 모듈, 빌드 시 .winmd  파일이 생성되며 uwp 프로젝트에서 임포트하여 사용할 수 있음.

## Setup
- cloning only applications
```
git clone https://github.com/IkbeomJeon/WiseUI.git
```
-  cloning applications and submodules
```
git clone --recurse-submodules https://github.com/IkbeomJeon/WiseUI.git
```


## Acknowledgement



## Lisense

To be updated.



