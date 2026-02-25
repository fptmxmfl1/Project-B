using UnityEditor;
using UnityEngine;

namespace ErrorAutoFixer
{
    /// <summary>
    /// API 키 및 기본 설정을 관리하는 설정 창
    /// </summary>
    public class SettingsWindow : EditorWindow
    {
        // === 내부 상태 ===
        private string apiKeyInput = string.Empty;
        private bool showAPIKey;
        private bool isTestingKey;
        private string testResultMessage = string.Empty;
        private bool? testResultSuccess;
        private bool autoCaptureEnabled;

        /// <summary>설정 창 열기</summary>
        [MenuItem("Tools/Error Auto Fixer/Settings", priority = 20)]
        public static void ShowWindow()
        {
            var window = GetWindow<SettingsWindow>("Error Auto Fixer 설정");
            window.minSize = new Vector2(400, 300);
        }

        private void OnEnable()
        {
            // 저장된 설정 로드
            apiKeyInput = ErrorFixerSettings.GetAPIKey();
            autoCaptureEnabled = ErrorFixerSettings.IsAutoCaptureEnabled();
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(10);

            // 제목
            EditorGUILayout.LabelField("Error Auto Fixer 설정", EditorStyles.boldLabel);
            EditorGUILayout.Space(5);

            DrawDivider();

            // API 키 섹션
            DrawAPIKeySection();

            EditorGUILayout.Space(10);
            DrawDivider();

            // 캡처 설정 섹션
            DrawCaptureSettings();

            EditorGUILayout.Space(10);
            DrawDivider();

            // 정보 섹션
            DrawAboutSection();
        }

        /// <summary>API 키 입력 및 테스트 UI</summary>
        private void DrawAPIKeySection()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Gemini API 키", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUILayout.BeginHorizontal();

            // API 키 입력 필드 (비밀번호 마스킹 토글)
            if (showAPIKey)
            {
                apiKeyInput = EditorGUILayout.TextField(apiKeyInput);
            }
            else
            {
                apiKeyInput = EditorGUILayout.PasswordField(apiKeyInput);
            }

            // 보기/숨기기 토글 버튼
            if (GUILayout.Button(showAPIKey ? "숨기기" : "보기", GUILayout.Width(60)))
            {
                showAPIKey = !showAPIKey;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            EditorGUILayout.BeginHorizontal();

            // 저장 버튼
            if (GUILayout.Button("저장", GUILayout.Height(25)))
            {
                ErrorFixerSettings.SetAPIKey(apiKeyInput);
                testResultMessage = "API 키가 저장되었습니다.";
                testResultSuccess = true;
            }

            // 테스트 버튼
            EditorGUI.BeginDisabledGroup(isTestingKey || string.IsNullOrEmpty(apiKeyInput));
            if (GUILayout.Button(isTestingKey ? "테스트 중..." : "연결 테스트", GUILayout.Height(25)))
            {
                TestAPIKey();
            }
            EditorGUI.EndDisabledGroup();

            EditorGUILayout.EndHorizontal();

            // 테스트 결과 메시지 표시
            if (!string.IsNullOrEmpty(testResultMessage))
            {
                EditorGUILayout.Space(3);
                var style = new GUIStyle(EditorStyles.helpBox);
                if (testResultSuccess == true)
                {
                    EditorGUILayout.HelpBox(testResultMessage, MessageType.Info);
                }
                else if (testResultSuccess == false)
                {
                    EditorGUILayout.HelpBox(testResultMessage, MessageType.Error);
                }
                else
                {
                    EditorGUILayout.HelpBox(testResultMessage, MessageType.None);
                }
            }

            // API 키 발급 안내
            EditorGUILayout.Space(3);
            if (GUILayout.Button("API 키 무료 발급받기 (Google AI Studio)", EditorStyles.linkLabel))
            {
                Application.OpenURL("https://aistudio.google.com");
            }
        }

        /// <summary>자동 캡처 설정 UI</summary>
        private void DrawCaptureSettings()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("에러 캡처 설정", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUI.BeginChangeCheck();
            autoCaptureEnabled = EditorGUILayout.Toggle("자동 에러 캡처", autoCaptureEnabled);
            if (EditorGUI.EndChangeCheck())
            {
                ErrorFixerSettings.SetAutoCapture(autoCaptureEnabled);

                // 캡처 상태 즉시 반영
                if (autoCaptureEnabled)
                    ErrorCapture.StartCapture();
                else
                    ErrorCapture.StopCapture();
            }

            EditorGUILayout.HelpBox(
                "활성화하면 에디터에서 발생하는 에러를 자동으로 수집합니다.",
                MessageType.Info);
        }

        /// <summary>버전 및 정보 표시</summary>
        private void DrawAboutSection()
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("정보", EditorStyles.boldLabel);
            EditorGUILayout.Space(3);

            EditorGUILayout.LabelField("버전", "0.1.0");
            EditorGUILayout.LabelField("모델", ErrorFixerSettings.GetModelName());
            EditorGUILayout.LabelField("캡처 상태", ErrorCapture.IsCapturing ? "캡처 중" : "중지됨");
            EditorGUILayout.LabelField("캡처된 에러 수", ErrorCapture.CapturedErrors.Count.ToString());
        }

        /// <summary>구분선 그리기</summary>
        private void DrawDivider()
        {
            var rect = EditorGUILayout.GetControlRect(false, 1);
            EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.3f));
        }

        /// <summary>API 키 연결 테스트 실행</summary>
        private void TestAPIKey()
        {
            isTestingKey = true;
            testResultMessage = "API 키 테스트 중...";
            testResultSuccess = null;

            GeminiAPIClient.TestAPIKey(apiKeyInput, (success, message) =>
            {
                isTestingKey = false;
                testResultSuccess = success;
                testResultMessage = message;
                Repaint();
            });
        }
    }
}
