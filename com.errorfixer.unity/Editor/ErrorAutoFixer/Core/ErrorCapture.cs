using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ErrorAutoFixer
{
    /// <summary>
    /// Unity 에디터의 에러 로그를 실시간 캡처하는 클래스
    /// [InitializeOnLoad]로 에디터 시작 시 자동 등록
    /// </summary>
    [InitializeOnLoad]
    public static class ErrorCapture
    {
        // === 상수 ===
        private const int MAX_CAPTURED_ERRORS = 100; // 최대 캡처 수

        // === 이벤트 ===
        /// <summary>새 에러가 캡처되었을 때 발생하는 이벤트</summary>
        public static event System.Action<CapturedError> OnErrorCaptured;

        // === 내부 저장소 ===
        private static readonly List<CapturedError> capturedErrors = new List<CapturedError>();
        private static readonly HashSet<int> errorHashes = new HashSet<int>(); // 중복 필터링용

        // === 공개 프로퍼티 ===

        /// <summary>캡처된 에러 목록 (읽기 전용)</summary>
        public static IReadOnlyList<CapturedError> CapturedErrors => capturedErrors;

        /// <summary>캡처 중 여부</summary>
        public static bool IsCapturing { get; private set; }

        // === static 생성자 (InitializeOnLoad) ===
        static ErrorCapture()
        {
            // 자동 캡처 설정이 켜져 있으면 시작
            if (ErrorFixerSettings.IsAutoCaptureEnabled())
            {
                StartCapture();
            }
        }

        // === 제어 ===

        /// <summary>캡처 시작 (이벤트 구독)</summary>
        public static void StartCapture()
        {
            if (IsCapturing) return;

            Application.logMessageReceived += OnLogReceived;
            IsCapturing = true;
        }

        /// <summary>캡처 중지 (이벤트 해제)</summary>
        public static void StopCapture()
        {
            if (!IsCapturing) return;

            Application.logMessageReceived -= OnLogReceived;
            IsCapturing = false;
        }

        /// <summary>캡처된 에러 목록 초기화</summary>
        public static void ClearErrors()
        {
            capturedErrors.Clear();
            errorHashes.Clear();
        }

        // === 내부 로직 ===

        /// <summary>
        /// 로그 메시지 수신 콜백
        /// Error, Exception, Assert 타입만 캡처
        /// </summary>
        private static void OnLogReceived(string message, string stackTrace, LogType type)
        {
            // Error, Exception, Assert만 캡처
            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
                return;

            // 중복 필터링 (동일 에러 메시지+스택 트레이스)
            int hash = ComputeErrorHash(message, stackTrace);
            if (errorHashes.Contains(hash))
                return;

            // 최대 캡처 수 초과 시 가장 오래된 항목 제거
            if (capturedErrors.Count >= MAX_CAPTURED_ERRORS)
            {
                var oldest = capturedErrors[0];
                int oldHash = ComputeErrorHash(oldest.message, oldest.stackTrace);
                errorHashes.Remove(oldHash);
                capturedErrors.RemoveAt(0);
            }

            // 스택 트레이스 파싱으로 파일/라인 정보 추출
            var parsedInfo = ErrorParser.Parse(message, stackTrace);

            // CapturedError 생성
            var error = new CapturedError
            {
                message = message,
                stackTrace = stackTrace,
                logType = type,
                timestamp = System.DateTime.Now,
                filePath = parsedInfo.hasFileInfo ? parsedInfo.filePath : null,
                lineNumber = parsedInfo.lineNumber,
                errorCode = parsedInfo.errorCode,
                isCompileError = parsedInfo.isCompileError,
                isAnalyzed = false,
                analysisResult = null
            };

            // 저장 및 이벤트 발행
            capturedErrors.Add(error);
            errorHashes.Add(hash);
            OnErrorCaptured?.Invoke(error);
        }

        /// <summary>
        /// 에러 메시지와 스택 트레이스의 해시값 계산 (중복 판별용)
        /// </summary>
        private static int ComputeErrorHash(string message, string stackTrace)
        {
            string combined = (message ?? string.Empty) + (stackTrace ?? string.Empty);
            return combined.GetHashCode();
        }
    }
}
