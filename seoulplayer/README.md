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

## 🎮 게임 씬 만들고 실행하기 (모든 게임 공통)

게임마다 **메뉴 한 번**으로 플레이 가능한 씬이 자동 생성됩니다. 순서는 모든 게임이 똑같아요.

1. **Unity Hub에서 `gameproject` 열기**
2. (최초 1회) TMP 창이 뜨면 → **`Import TMP Essentials`** 클릭
3. 상단 메뉴에서 **해당 게임의 `플레이 가능한 씬 생성`** 클릭
   - 별마당도서관: **`별마당도서관 > 플레이 가능한 씬 생성`**
   - 서울어린이대공원: **`서울대공원 > 플레이 가능한 씬 생성`**
   - → 씬·임시에셋이 `Games/<게임명>/` 안에 자동 생성됩니다
4. **`서울플레이업 > 한글 폰트 적용`** 클릭 (한글 깨짐 □ 방지, 최초/씬별 1회)
5. ▶ **Play** 로 실행

> 🧩 **새 게임을 만들 때도 같은 방식**입니다. 자기 게임용 빌더(에디터 스크립트)를 `Games/<게임명>/Editor/` 에 만들고, 위 3~5번처럼 메뉴로 씬을 생성하면 돼요.
> 빌더 없이 **수동으로 씬을 구성하는 방법**은 [docs/도서관게임_셋업가이드.md](docs/도서관게임_셋업가이드.md)를 참고하세요 (한 번 익히면 어느 게임이든 동일).

> ⚠️ 씬을 새로 생성하면 그 게임의 기존 씬을 덮어씁니다. 작업 중인 씬이 있으면 주의하세요.

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

## 📦 이미지·에셋이 안 보여요? → Git LFS 받기

이 프로젝트의 **이미지·폰트·사운드는 Git LFS**로 관리됩니다. **LFS 없이 클론하면 이미지 대신 "포인터 텍스트 파일"만 받아져서**, Unity 씬이 텅 비어 보이거나 스프라이트가 안 뜹니다.

**해결 (클론한 폴더 안에서):**
```bash
git lfs install
git lfs pull
```
→ 그다음 **Unity 창을 클릭(포커스)** 하면 이미지가 다시 임포트되어 보입니다.

**LFS 문제인지 확인:** 이미지 `.png` 하나를 메모장으로 열었을 때 아래처럼 **글자만** 나오면 = 아직 안 받아진 것 → 위 명령으로 해결.
```
version https://git-lfs.github.com/spec/v1
oid sha256:...
size ...
```

**제일 확실한 방법:** `git lfs install` 을 **먼저** 한 뒤 `git clone` 하면 처음부터 이미지까지 받아집니다.

> 자동 LFS 대상: `.png .jpg .jpeg .psd .gif .tga .wav .mp3 .ogg .fbx .glb .gltf .obj .ttf .otf`
> 팀원 **전원**이 최초 1회 `git lfs install` 필수!

## ⚠️ 협업 주의사항

- **씬(.unity)·프리팹은 한 번에 한 사람만** 수정하세요 (충돌 방지).
- `.meta` 파일은 **항상 같이 커밋**하세요. 절대 지우거나 무시하지 마세요.
- Unity 설정: `Edit > Project Settings > Editor`
  - Asset Serialization Mode → **Force Text**
  - Version Control Mode → **Visible Meta Files**
