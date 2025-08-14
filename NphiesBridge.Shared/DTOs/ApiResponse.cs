using System;
using System.Collections.Generic;

namespace NphiesBridge.Shared.DTOs
{
    /// <summary>
    /// Generic API response envelope.
    /// This merged version preserves all legacy behaviors and adds convenience helpers
    /// without breaking existing callers.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        // LEGACY: keep default message empty to avoid changing existing behavior
        public static ApiResponse<T> SuccessResult(T data, string message = "")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        // NEW-FRIENDLY: convenience helper that returns a success with a default message
        public static ApiResponse<T> SuccessWithDefaultMessage(T data, string defaultMessage = "Operation completed successfully")
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = defaultMessage
            };
        }

        // LEGACY: single-error factory
        public static ApiResponse<T> ErrorResult(string error)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Errors = new List<string> { error }
            };
        }

        // LEGACY: multiple-errors factory
        public static ApiResponse<T> ErrorResult(List<string> errors)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Errors = errors ?? new List<string>()
            };
        }

        // NEW: message + optional errors (kept alongside legacy overloads)
        public static ApiResponse<T> ErrorResult(string message, List<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message ?? string.Empty,
                Errors = errors ?? new List<string>()
            };
        }

        // Convenience: build from exception while keeping a friendly message
        public static ApiResponse<T> FromException(Exception ex, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message ?? "An error occurred.",
                Errors = new List<string> { ex.Message }
            };
        }
    }

    /// <summary>
    /// Non-generic convenience type when no data payload is needed.
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        // Keep the previous default success message here as provided in your version
        public static ApiResponse SuccessResult(string message = "Operation completed successfully")
        {
            return new ApiResponse
            {
                Success = true,
                Message = message
            };
        }

        // Mirror legacy + new error factories; mark as 'new' to explicitly hide base static overloads.
        public static new ApiResponse ErrorResult(string error)
        {
            return new ApiResponse
            {
                Success = false,
                Errors = new List<string> { error }
            };
        }

        public static new ApiResponse ErrorResult(List<string> errors)
        {
            return new ApiResponse
            {
                Success = false,
                Errors = errors ?? new List<string>()
            };
        }

        public static new ApiResponse ErrorResult(string message, List<string>? errors = null)
        {
            return new ApiResponse
            {
                Success = false,
                Message = message ?? string.Empty,
                Errors = errors ?? new List<string>()
            };
        }
    }

    // Generic success payload (used by some endpoints/services)
    public class SuccessResponse
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
    }

    // Generic error payload (used by some endpoints/services)
    public class ErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public List<string> Errors { get; set; } = new();
    }
}