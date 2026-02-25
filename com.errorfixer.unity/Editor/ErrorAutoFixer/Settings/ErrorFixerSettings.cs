using UnityEditor;

namespace ErrorAutoFixer
{
    /// <summary>
    /// API 키 및 패키지 설정을 관리하는 정적 클래스
    /// EditorPrefs를 사용하여 로컬 머신에만 저장 (보안)
    /// </summary>
    public static class ErrorFixerSettings
    {
        // === EditorPrefs 키 상수 ===
        private const string API_KEY_PREF = "ErrorAutoFixer_GeminiAPIKey";
        private const string AUTO_CAPTURE_PREF = "ErrorAutoFixer_AutoCapture";

        // === Gemini API 상수 ===
        private const string GEMINI_MODEL = "gemini-2.5-flash";
        private const string GEMINI_API_BASE_URL =
            "https://generativelanguage.googleapis.com/v1beta/models/";

        // === API 키 관리 ===

        /// <summary>API 키 저장</summary>
        public static void SetAPIKey(string apiKey)
        {
            EditorPrefs.SetString(API_KEY_PREF, apiKey ?? string.Empty);
        }

        /// <summary>저장된 API 키 조회 (없으면 빈 문자열)</summary>
        public static string GetAPIKey()
        {
            return EditorPrefs.GetString(API_KEY_PREF, string.Empty);
        }

        /// <summary>API 키 설정 여부 확인</summary>
        public static bool HasAPIKey()
        {
            return !string.IsNullOrEmpty(GetAPIKey());
        }

        // === 자동 캡처 설정 ===

        /// <summary>에러 자동 캡처 ON/OFF 설정</summary>
        public static void SetAutoCapture(bool enabled)
        {
            EditorPrefs.SetBool(AUTO_CAPTURE_PREF, enabled);
        }

        /// <summary>자동 캡처 활성화 여부 조회 (기본값: true)</summary>
        public static bool IsAutoCaptureEnabled()
        {
            return EditorPrefs.GetBool(AUTO_CAPTURE_PREF, true);
        }

        // === Gemini API 엔드포인트 ===

        /// <summary>API 호출 URL 생성 (모델명 + API 키 포함)</summary>
        public static string GetAPIEndpoint()
        {
            string apiKey = GetAPIKey();
            return $"{GEMINI_API_BASE_URL}{GEMINI_MODEL}:generateContent?key={apiKey}";
        }

        /// <summary>사용 중인 모델명 조회</summary>
        public static string GetModelName()
        {
            return GEMINI_MODEL;
        }
    }
}
