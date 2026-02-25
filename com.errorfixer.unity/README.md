# Error Auto Fixer

Unity 에디터용 에러 자동 분석 및 수정 패키지입니다.

## 기능
- Unity 에디터 내 에러 로그 자동 캡처
- Google Gemini API를 통한 에러 원인 분석
- 자동 수정 가능한 에러는 원클릭 패치 제공
- 자동 수정이 어려운 에러는 원인 진단 텍스트 안내

## 설치
Unity Package Manager에서 Git URL로 추가:
```
https://github.com/your-repo/com.errorfixer.unity.git
```

또는 로컬 경로로 추가:
```json
"com.errorfixer.unity": "file:경로/com.errorfixer.unity"
```

## 사용법
1. `Tools > Error Auto Fixer > Settings`에서 Gemini API 키 입력
2. 에러 발생 시 `Tools > Error Auto Fixer > Open Window` 열기
3. 에러 선택 후 [분석] 버튼 클릭

## 요구사항
- Unity 2021.3 LTS 이상
- Google Gemini API 키 (무료 발급: https://aistudio.google.com)
