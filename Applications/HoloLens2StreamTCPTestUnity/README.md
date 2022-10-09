# **HoloLens2StreamTCPTestUnity**



### Overview

- [이 프로젝트](https://github.com/petergu684/HoloLens2-ResearchMode-Unity)를 참고하여 작성 됨

- Assets/[Scenes](https://github.com/IkbeomJeon/WiseUI/tree/master/Applications/HoloLens2StreamTCPTestUnity/Assets/Scenes)

  - HoloLens2 PV Camera Test.unity
    PV(Photo/Video) Camera 로 받아온 영상을 Visualization하고 TCP 서버로 전송하는 예제.
    
  - 하이라키에 있는 'HL2StreamReader' 게임오브젝트를 선택하고 서버 Ip와 port를 입력.

    

- [TCPServerPython](https://github.com/IkbeomJeon/WiseUI/tree/master/Applications/HoloLens2StreamTCPTestUnity/TCPServerPython)

  - 홀로렌즈와 통신하는 TCP 서버. 실행 시 접속 대기 상태가 되며 클라이언트(홀로렌즈)가 접속하면 실시간으로 획득한 데이터를 출력함. 연결이 끊기면 다시 접속 대기 상태로 전환 됨.
    

### Compatibility

- Unity 2020.3.21f1 (LTS)*
- Visual Studio 2019

\* To use it in Unity 2020.1 - 2021.1,



- Open Unity project and install XRSDK (Project Settings-XR Plugin Management-install, then tick "Windows Mixed Reality")
  
- Select MixedRealityToolkit Gameobject in the Hierarchy. In the Inspector, change the mixed reality configuration profile to `New XRSDKConfigurationProfile` (or `DefaultXRSDKConfigurationProfile`).
  
- Point cloud sample not supported in Unity 2021.2 or later since OpenXR becomes the only supported pipeline with different way of obtaining the Unity world coordiante frame. Other functions shouldn't be influenced.
  
### Build 

1. Open this folder in Unity.
2. Go to Build Settings, switch target platform to UWP.
3. In the Project tab, open `Scenes/HoloLens2 PV Camera Test.unity`.

4. Hopefully, there is no error in the console. Go to Build Settings, change Target Device to HoloLens, Architecture to ARM64. Build the Unity project in a new folder (e.g. App folder).
5. After building the visual studio solution from Unity, go to `[Project name]/Package.appxmanifest` and modify the <package>...</package> and <Capabilities>...</Capabilities> to the manifest file, like this.

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

6. Save the changes. Open `MyProject.sln`. Change the build type to Release/Master-ARM64-Device(or Remote Machine). Build - Deploy.
   

### Note

- The app may not function properly the first time you open the deployed app when there are pop-up windows asking for permissions. You can simply grant the permissions, close the app and reopen it. Then everything should be fine.
  
- You need to restart the device (hold the power button for several seconds) each time the device hiberates after you opened an app that uses research mode functions. So if your app suddenly cannot get any sensor data, try restarting your device. Please let me know if you know how to solve this issue.