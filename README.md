# 🎮 서울플레이어 (Seoul Player)

> AI 협업으로 만드는 Unity 게임 프로젝트

## 🐣 깃허브 연결 전에 꼭 읽기!!!!!!!!!!!!!!!!!!!!!!!!!!!!1

>**(docs/깃허브_왕초보_가이드.md)**

## 📖 프로젝트 문서

- **🐣 깃허브 왕초보 가이드:** [docs/깃허브_왕초보_가이드.md](docs/깃허브_왕초보_가이드.md)
- **기획서 (Notion):** https://gaudy-activity-72e.notion.site/1-AI-357a9134e9cb803d99f3cb2ae6f2787c
- **개발 컨벤션:** [docs/컨벤션.md](docs/컨벤션.md)
- **작업 분담:** [docs/작업분담.md](docs/작업분담.md)

## 🛠 개발 환경

- **엔진:** Unity (URP - Universal Render Pipeline)
- **입력:** Unity Input System
- **언어:** C#

> ⚠️ Unity 버전을 팀원 모두 **동일하게** 맞추세요. 다르면 프로젝트가 깨질 수 있습니다.
> (버전 확인: `gameproject/ProjectSettings/ProjectVersion.txt`)

## 📁 폴더 구조 (게임별로 정리)

에셋은 **서울 명소 게임별 폴더**로 나뉘어 있습니다. 각자 자기 게임 폴더 안에서 작업하세요.

```
seoulplayer/
├─ docs/                       # 기획·컨벤션·가이드 문서
└─ gameproject/                # Unity 프로젝트
   ├─ Assets/
   │  ├─ Games/                # ⭐ 모든 게임이 여기 (게임별 폴더)
   │  │  ├─ 별마당도서관/
   │  │  │  ├─ Scenes/         # 이 게임 씬
   │  │  │  ├─ Scripts/        # 이 게임 스크립트
   │  │  │  ├─ Sprites/  Prefabs/  Audios/  Data/
   │  │  │  └─ Editor/         # 이 게임 에디터 스크립트(빌더 등)
   │  │  ├─ 서울어린이대공원/   # (위와 동일 구성)
   │  │  ├─ 경복궁/ 남산/ 홍대/ … # 나머지 명소(앞으로 채울 폴더)
   │  │  └─ _공통/             # 여러 게임 공유(CameraFollow, 폰트 등)
   │  ├─ Settings/             # URP 설정 (Unity 필수 · 수정 금지)
   │  └─ TextMesh Pro/         # TMP (Unity 필수)
   ├─ Packages/
   └─ ProjectSettings/
```

> 💡 게임 폴더 목록·매핑은 [Assets/Games/README.md](gameproject/Assets/Games/README.md) 참고.
> Unity는 스크립트/씬을 **GUID로 찾으므로** 폴더 위치는 자유예요. 위 구조는 "사람이 보기 편하라고" 정한 규칙입니다.

## 🚀 처음 시작하기 (Setup)

```bash
# 0. Git LFS 설치 (최초 1회, 필수!) — 이미지·사운드·3D 모델 관리용
#    안 하면 에셋이 깨진 포인터 파일로 보입니다.
brew install git-lfs   # (Windows는 https://git-lfs.com 에서 설치)
git lfs install

# 1. 내 포크를 클론
git clone https://github.com/<내아이디>/seoulplayer
cd seoulplayer

# 2. 원본 저장소를 upstream으로 연결
git remote add upstream https://github.com/mingyo07/seoulplayer

# 3. Unity Hub에서 gameproject 폴더 열기
```

> 📦 **Git LFS 사용 중:** `.png .jpg .psd .wav .mp3 .fbx .glb .gltf .obj .ttf` 등은 자동으로 LFS로 관리됩니다. 팀원 전원이 위 `git lfs install`을 꼭 실행하세요.

## 🔄 협업 흐름 (Fork & PR)

```bash
# 작업 전: 원본 최신 받아오기
git checkout main
git pull upstream main

# 내 작업 브랜치에서 작업
git checkout -b feature/기능이름
# ... 작업 ...
git add .
git commit -m "feat: 작업 내용"
git push origin feature/기능이름

# GitHub에서 Pull Request 생성 → 리뷰 → 병합
```

자세한 규칙은 [docs/컨벤션.md](docs/컨벤션.md) 참고.

## ⚠️ 협업 주의사항

- **씬(.unity)·프리팹은 한 번에 한 사람만** 수정하세요 (충돌 방지).
- `.meta` 파일은 **항상 같이 커밋**하세요. 절대 지우거나 무시하지 마세요.
- Unity 설정: `Edit > Project Settings > Editor`
  - Asset Serialization Mode → **Force Text**
  - Version Control Mode → **Visible Meta Files**
