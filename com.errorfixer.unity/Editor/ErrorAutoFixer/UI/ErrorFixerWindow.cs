using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ErrorAutoFixer
{
    /// <summary>
    /// Error Auto Fixer 메인 에디터 창
    /// 에러 목록 패널 + 분석 결과 패널 2단 구성
    /// </summary>
    public class ErrorFixerWindow : EditorWindow
    {
        // === 상수 ===
        private const float ERROR_LIST_WIDTH_RATIO = 0.4f; // 에러 목록 패널 너비 비율

        // === 내부 상태 ===
        private Vector2 errorListScrollPos;
        private Vector2 resultScrollPos;
        private int selectedErrorIndex = -1;
        private bool isAnalyzing;
        private string analysisErrorMessage;

        // === 스타일 캐시 ===
        private GUIStyle headerStyle;
        private GUIStyle errorItemStyle;
        private GUIStyle selectedItemStyle;
        private GUIStyle diagnosisStyle;
        private GUIStyle trackBadgeStyle;
        private bool stylesInitialized;

        // === 메뉴 등록 ===

        /// <summary>메인 창 열기</summary>
        [MenuItem("Tools/Error Auto Fixer/Open Window", priority = 10)]
        public static void ShowWindow()
        {
            var window = GetWindow<ErrorFixerWindow>("Error Auto Fixer");
            window.minSize = new Vector2(700, 400);
        }

        // === EditorWindow 라이프사이클 ===

        private void OnEnable()
        {
            // 에러 캡처 이벤트 구독 (새 에러 발생 시 UI 갱신)
            ErrorCapture.OnErrorCaptured += OnNewErrorCaptured;
        }

        private void OnDisable()
        {
            ErrorCapture.OnErrorCaptured -= OnNewErrorCaptured;
        }

        /// <summary>새 에러 캡처 시 UI 갱신</summary>
        private void OnNewErrorCaptured(CapturedError error)
        {
            Repaint();
        }

        private void OnGUI()
        {
            InitStyles();

            // 상단 툴바
            DrawToolbar();

            // API 키 미설정 경고
            if (!ErrorFixerSettings.HasAPIKey())
            {
                DrawAPIKeyWarning();
            }

            // 메인 영역: 에러 목록(좌) + 분석 결과(우)
            EditorGUILayout.BeginHorizontal();

            // 좌측: 에러 목록 패널
            float listWidth = position.width * ERROR_LIST_WIDTH_RATIO;
            EditorGUILayout.BeginVertical(GUILayout.Width(listWidth));
            DrawErrorListPanel();
            EditorGUILayout.EndVertical();

            // 구분선
            DrawVerticalDivider();

            // 우측: 분석 결과 패널
            EditorGUILayout.BeginVertical();
            DrawResultPanel();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndHorizontal();
        }

        // === 스타일 초기화 ===

        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 12
            };

            errorItemStyle = new GUIStyle("box")
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin = new RectOffset(2, 2, 1, 1),
                alignment = TextAnchor.UpperLeft,
                wordWrap = true
            };

            selectedItemStyle = new GUIStyle(errorItemStyle);
            selectedItemStyle.normal.background = MakeTexture(2, 2, new Color(0.24f, 0.49f, 0.91f, 0.3f));

            diagnosisStyle = new GUIStyle(EditorStyles.wordWrappedLabel)
            {
                richText = true,
                padding = new RectOffset(5, 5, 5, 5)
            };

            trackBadgeStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };

            stylesInitialized = true;
        }

        // === UI 렌더링 ===

        /// <summary>상단 툴바</summary>
        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 캡처 상태 표시
            string captureStatus = ErrorCapture.IsCapturing ? "● 캡처 중" : "○ 중지됨";
            GUILayout.Label(captureStatus, GUILayout.Width(70));

            GUILayout.FlexibleSpace();

            // 설정 버튼
            if (GUILayout.Button("설정", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                SettingsWindow.ShowWindow();
            }

            // 초기화 버튼
            if (GUILayout.Button("초기화", EditorStyles.toolbarButton, GUILayout.Width(45)))
            {
                OnClearClicked();
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>API 키 미설정 경고 배너</summary>
        private void DrawAPIKeyWarning()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            EditorGUILayout.LabelField("⚠ API 키가 설정되지 않았습니다.", EditorStyles.wordWrappedLabel);
            if (GUILayout.Button("설정 열기", GUILayout.Width(80)))
            {
                SettingsWindow.ShowWindow();
            }
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>좌측: 에러 목록 패널</summary>
        private void DrawErrorListPanel()
        {
            EditorGUILayout.LabelField("에러 목록", headerStyle);
            EditorGUILayout.Space(3);

            var errors = ErrorCapture.CapturedErrors;

            if (errors.Count == 0)
            {
                EditorGUILayout.HelpBox("캡처된 에러가 없습니다.\n에러가 발생하면 여기에 표시됩니다.", MessageType.Info);
                return;
            }

            errorListScrollPos = EditorGUILayout.BeginScrollView(errorListScrollPos);

            // 에러 목록을 최신순으로 표시
            for (int i = errors.Count - 1; i >= 0; i--)
            {
                DrawErrorItem(errors[i], i);
            }

            EditorGUILayout.EndScrollView();

            // 분석 버튼
            EditorGUILayout.Space(5);
            EditorGUI.BeginDisabledGroup(
                selectedErrorIndex < 0 ||
                isAnalyzing ||
                !ErrorFixerSettings.HasAPIKey() ||
                GeminiAPIClient.IsRequesting);

            string buttonText = isAnalyzing ? "분석 중..." : "분석";
            if (GUILayout.Button(buttonText, GUILayout.Height(30)))
            {
                OnAnalyzeClicked();
            }

            EditorGUI.EndDisabledGroup();
        }

        /// <summary>에러 항목 하나를 렌더링</summary>
        private void DrawErrorItem(CapturedError error, int index)
        {
            bool isSelected = (index == selectedErrorIndex);
            var style = isSelected ? selectedItemStyle : errorItemStyle;

            EditorGUILayout.BeginVertical(style);

            // 첫 줄: 시간 + 에러 메시지 요약
            EditorGUILayout.BeginHorizontal();

            // 분석 상태 아이콘
            string statusIcon = error.isAnalyzed ? "●" : "○";
            GUILayout.Label(statusIcon, GUILayout.Width(12));

            // 시간
            string time = error.timestamp.ToString("HH:mm:ss");
            GUILayout.Label(time, EditorStyles.miniLabel, GUILayout.Width(55));

            // 에러 메시지 (줄임)
            string shortMessage = TruncateMessage(error.message, 50);
            GUILayout.Label(shortMessage, EditorStyles.miniLabel);

            EditorGUILayout.EndHorizontal();

            // 둘째 줄: 파일/라인 정보
            if (!string.IsNullOrEmpty(error.filePath))
            {
                string fileName = System.IO.Path.GetFileName(error.filePath);
                string fileInfo = error.lineNumber > 0
                    ? $"  {fileName}:{error.lineNumber}"
                    : $"  {fileName}";
                EditorGUILayout.LabelField(fileInfo, EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();

            // 클릭 감지
            var lastRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && lastRect.Contains(Event.current.mousePosition))
            {
                selectedErrorIndex = index;
                Event.current.Use();
                Repaint();
            }
        }

        /// <summary>우측: 분석 결과 패널</summary>
        private void DrawResultPanel()
        {
            EditorGUILayout.LabelField("분석 결과", headerStyle);
            EditorGUILayout.Space(3);

            // 에러 미선택
            if (selectedErrorIndex < 0 || selectedErrorIndex >= ErrorCapture.CapturedErrors.Count)
            {
                EditorGUILayout.HelpBox("좌측 목록에서 에러를 선택하세요.", MessageType.Info);
                return;
            }

            var error = ErrorCapture.CapturedErrors[selectedErrorIndex];

            resultScrollPos = EditorGUILayout.BeginScrollView(resultScrollPos);

            // 에러 원문 표시
            EditorGUILayout.LabelField("에러 메시지", EditorStyles.boldLabel);
            EditorGUILayout.SelectableLabel(error.message, EditorStyles.wordWrappedLabel,
                GUILayout.MinHeight(40));

            EditorGUILayout.Space(5);

            // 분석 중 로딩 표시
            if (isAnalyzing)
            {
                DrawLoadingIndicator();
                EditorGUILayout.EndScrollView();
                return;
            }

            // 에러 메시지 표시
            if (!string.IsNullOrEmpty(analysisErrorMessage))
            {
                EditorGUILayout.HelpBox(analysisErrorMessage, MessageType.Error);
            }

            // 분석 결과 표시
            if (error.isAnalyzed && error.analysisResult != null)
            {
                DrawAnalysisResult(error.analysisResult);
            }
            else if (!error.isAnalyzed)
            {
                EditorGUILayout.HelpBox("[분석] 버튼을 클릭하여 이 에러를 분석하세요.", MessageType.Info);
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>분석 결과 상세 표시</summary>
        private void DrawAnalysisResult(AnalysisResult result)
        {
            // Track 배지 + 확신도
            EditorGUILayout.BeginHorizontal();
            string trackLabel = result.fixable ? "Track A: 수정 가능" : "Track B: 진단만 제공";
            Color trackColor = result.fixable ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.9f, 0.6f, 0.1f);
            DrawBadge(trackLabel, trackColor);

            string confLabel = $"확신도: {result.confidence}";
            Color confColor = result.confidence == "high" ? new Color(0.2f, 0.7f, 0.3f)
                : result.confidence == "medium" ? new Color(0.9f, 0.6f, 0.1f)
                : new Color(0.9f, 0.3f, 0.3f);
            DrawBadge(confLabel, confColor);

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            // 진단
            EditorGUILayout.LabelField("진단", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(result.diagnosis, diagnosisStyle);

            EditorGUILayout.Space(5);

            // 파일 정보
            if (!string.IsNullOrEmpty(result.file))
            {
                EditorGUILayout.LabelField("파일 정보", EditorStyles.boldLabel);
                string fileInfo = result.line > 0 ? $"{result.file}:{result.line}" : result.file;
                EditorGUILayout.SelectableLabel(fileInfo, GUILayout.Height(18));
                EditorGUILayout.Space(5);
            }

            // 해결 방법
            if (!string.IsNullOrEmpty(result.solution))
            {
                EditorGUILayout.LabelField("해결 방법", EditorStyles.boldLabel);
                EditorGUILayout.LabelField(result.solution, diagnosisStyle);
                EditorGUILayout.Space(5);
            }

            // Track A: 패치 정보 미리보기 (Phase 1에서는 텍스트만 표시)
            if (result.fixable && result.patch != null)
            {
                EditorGUILayout.LabelField("수정 제안", EditorStyles.boldLabel);

                // 수정 전
                EditorGUILayout.LabelField("수정 전:", EditorStyles.miniLabel);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.SelectableLabel(result.patch.original,
                    EditorStyles.wordWrappedLabel, GUILayout.MinHeight(20));
                EditorGUILayout.EndVertical();

                // 수정 후
                EditorGUILayout.LabelField("수정 후:", EditorStyles.miniLabel);
                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.SelectableLabel(result.patch.fixedCode,
                    EditorStyles.wordWrappedLabel, GUILayout.MinHeight(20));
                EditorGUILayout.EndVertical();

                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(
                    "자동 수정 기능은 Phase 2에서 제공됩니다. 위 수정 제안을 참고하여 수동으로 수정해주세요.",
                    MessageType.Info);
            }
        }

        /// <summary>로딩 인디케이터</summary>
        private void DrawLoadingIndicator()
        {
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("Gemini API로 분석 중...", EditorStyles.centeredGreyMiniLabel);

            // 간단한 애니메이션 효과 (Repaint 유도)
            Repaint();
        }

        // === 액션 메서드 ===

        /// <summary>[분석] 버튼 클릭 처리</summary>
        private void OnAnalyzeClicked()
        {
            if (selectedErrorIndex < 0 || selectedErrorIndex >= ErrorCapture.CapturedErrors.Count)
                return;

            var error = ErrorCapture.CapturedErrors[selectedErrorIndex];

            isAnalyzing = true;
            analysisErrorMessage = null;

            // 소스 코드 읽기 (파일 정보가 있는 경우)
            string sourceCode = null;
            if (!string.IsNullOrEmpty(error.filePath))
            {
                var context = SourceCodeReader.ReadContext(error.filePath, error.lineNumber);
                if (context.fileExists)
                {
                    // 전체 소스가 있으면 전체를, 아니면 주변 코드를 전송
                    sourceCode = context.fullSource ?? context.surroundingCode;
                }
            }

            // Gemini API 호출
            GeminiAPIClient.AnalyzeError(error, sourceCode,
                result =>
                {
                    // 성공: 분석 결과 저장
                    error.analysisResult = result;
                    error.isAnalyzed = true;
                    isAnalyzing = false;
                    Repaint();
                },
                errorMessage =>
                {
                    // 실패: 에러 메시지 표시
                    isAnalyzing = false;
                    analysisErrorMessage = errorMessage;
                    Repaint();
                }
            );

            Repaint();
        }

        /// <summary>[초기화] 버튼 클릭 처리</summary>
        private void OnClearClicked()
        {
            ErrorCapture.ClearErrors();
            selectedErrorIndex = -1;
            analysisErrorMessage = null;
            Repaint();
        }

        // === 유틸리티 ===

        /// <summary>메시지를 지정 길이로 줄임</summary>
        private static string TruncateMessage(string message, int maxLength)
        {
            if (string.IsNullOrEmpty(message)) return string.Empty;
            if (message.Length <= maxLength) return message;

            // 줄바꿈 이전까지만
            int newlineIndex = message.IndexOf('\n');
            if (newlineIndex > 0 && newlineIndex < maxLength)
                return message.Substring(0, newlineIndex) + "...";

            return message.Substring(0, maxLength) + "...";
        }

        /// <summary>단색 텍스처 생성 (선택 하이라이트용)</summary>
        private static Texture2D MakeTexture(int width, int height, Color color)
        {
            var pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            var texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>컬러 배지 그리기</summary>
        private void DrawBadge(string text, Color color)
        {
            var prevColor = GUI.backgroundColor;
            GUI.backgroundColor = color;

            GUILayout.Label(text, "button", GUILayout.Height(20));

            GUI.backgroundColor = prevColor;
        }

        /// <summary>수직 구분선</summary>
        private void DrawVerticalDivider()
        {
            var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(1));
            rect.height = position.height;
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }
    }
}
