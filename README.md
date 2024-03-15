# MultiRacing_Client
>Youtube URL : https://www.youtube.com/watch?v=VpqVrVNnLuA

안녕하세요.  
[Unity] 포트폴리오 용으로 제작한 멀티레이싱 게임 입니다.  
해당 프로젝트는 클라이언트 파트입니다.

## 주요 스크립트 경로
./Scripts/Chatting/SC_Client.cs  
채팅방 입장 시, TCP 소켓 연결  <hr/>
./Scripts/InGame/MultiplayInGame/SC_MultiplayClient   
인게임 입장 시, 소켓파일 동일 및 수신 쓰레드만 변경  <hr/>
./Scripts/Login/SC_loginmanager.cs  
필요 시 UnityWebRequest 사용하여 HTTP 통신

## 특이사항
프로토콜 설계 : TCP 소켓 연결 + 수신 Thread