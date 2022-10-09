# WiseUI Integration 
ARRC-UVR Wise UI 통합 프로젝트



## Contents

- Application/
  - [WiseUIAppUnity](https://github.com/IkbeomJeon/WiseUI/tree/master/Applications/WiseUIAppUnity) 
    - Tcp프로토콜을 이용해서 HoloLens2의 실시간 센싱 값들을 서버에 전송하고 처리된 결과를 Visualization하는 유니티 프로젝트.

  - [HttpCommunicationTestUnity](https://github.com/IkbeomJeon/WiseUI/tree/master/Applications/HttpCommunicationTestUnity)
    - Http 프로토콜을 이용해서 Sample Image 한장을 전송하고 처리된 결과를 수신받는 example.(아직 HoloLens2 입력으로 테스트 못함.)

  - [TCPServerPython](https://github.com/IkbeomJeon/WiseUI/tree/master/Applications/TCPServerPython)
    - 홀로렌즈2에서 전송한 데이터를 수신하고 출력하는 Tcp 서버. 스크립트 실행 시 접속 대기 상태가 되며 클라이언트(홀로렌즈)가 접속하면 매 프레임마다 획득한 데이터를 출력함. 연결이 끊기면 다시 접속 대기 상태로 전환 됨.

 
  
- Modules/

  - [ARRCObjectron](https://gitlab.com/IkbeomJeon/arrcobjectron)
    - 3D Object pose estimation 모듈

  - [HoloLens2Stream](https://github.com/IkbeomJeon/HoloLens2Stream)
    - HoloLens2 Stream에 접근하여 각종 센서 값을 받아오는 모듈
    
  - [wjCho, HandTracker]()
    - To be updated

  - [jwJeon, SLAM]()
    - To be updated



## Setup

- Cloning only applications
```
git clone https://github.com/IkbeomJeon/WiseUI.git
```


-  Cloning applications and all submodules

```
git clone --recurse-submodules https://github.com/IkbeomJeon/WiseUI.git
```



## Compatibility

- Unity 2020.3.21f1 (LTS)*
- Visual Studio 2019

\* To use it in Unity 2020.1 - 2021.1,



Point cloud sample not supported in Unity 2021.2 or later since OpenXR becomes the only supported pipeline with different way of obtaining the Unity world coordiante frame. Other functions shouldn't be influenced.




## Build 

1. Open Application/[HoloLens2StreamTCPTestUnity](https://github.com/IkbeomJeon/WiseUI/tree/master/Applications/HoloLens2StreamTCPTestUnity)  in Unity.
2. Install XRSDK (Project Settings-XR Plugin Management-install, then tick "Windows Mixed Reality")
3. In the Project tab, open `Scenes/HoloLens2 PV Camera Test.unity`.
4. Select MixedRealityToolkit Gameobject in the Hierarchy. In the Inspector, change the mixed reality configuration profile to `New XRSDKConfigurationProfile` (or `DefaultXRSDKConfigurationProfile`).
5. Go to Build Settings, switch target platform to UWP.
6. Hopefully, there is no error in the console. Go to Build Settings, change Target Device to HoloLens, Architecture to ARM64. Build the Unity project in a new folder (e.g. App folder).
7. After building the visual studio solution from Unity, go to `[Project name]/Package.appxmanifest` and modify the <package>...</package> and <Capabilities>...</Capabilities>  like this.

```xml 
<Package 
  xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest" 
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" 
  xmlns:uap2="http://schemas.microsoft.com/appx/manifest/uap/windows10/2" 
  xmlns:uap3="http://schemas.microsoft.com/appx/manifest/uap/windows10/3" 
  xmlns:uap4="http://schemas.microsoft.com/appx/manifest/uap/windows10/4" 
  xmlns:iot="http://schemas.microsoft.com/appx/manifest/iot/windows10" 
  xmlns:mobile="http://schemas.microsoft.com/appx/manifest/mobile/windows10" 
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities" 
  IgnorableNamespaces="uap uap2 uap3 uap4 mp mobile iot rescap" 
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"> 
```

```xml
  <Capabilities>
    <rescap:Capability Name="perceptionSensorsExperimental" />
    <Capability Name="internetClient" />
    <Capability Name="internetClientServer" />
    <Capability Name="privateNetworkClientServer" />
    <uap2:Capability Name="spatialPerception" />
    <DeviceCapability Name="backgroundSpatialPerception"/>
    <DeviceCapability Name="webcam" />
  </Capabilities>
```

`<DeviceCapability Name="backgroundSpatialPerception"/>` is only necessary if you use IMU sensor. 

6. Save the changes. Open `[Project name].sln`. Change the build type to Release/Master-ARM64-Device(or Remote Machine). Build - Deploy.



## Acknowledgement

- HoloLens2 Research Mode Stream 라이브러리는 [이 프로젝트](https://github.com/petergu684/HoloLens2-ResearchMode-Unity)를 참고하여 작성 됨.





## Lisense

To be updated.



