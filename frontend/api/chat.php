<?php
declare(strict_types=1);

// --- Configuration ---
$config = require __DIR__ . '/config.php';
$backendUrl = $config['backend_url'];    // e.g. https://resume-chat.k8s.example.com
$apiKey     = $config['api_key'];        // shared GUID
$rateLimit  = $config['rate_limit'];     // requests per window
$rateWindow = $config['rate_window'];    // window in seconds
$canary     = $config['canary'] ?? '';   // prompt injection sentinel

// --- Rate Limiting (session-based) ---
session_start();

$now = time();
if (!isset($_SESSION['rate_window_start']) || $now - $_SESSION['rate_window_start'] >= $rateWindow) {
    $_SESSION['rate_count'] = 0;
    $_SESSION['rate_window_start'] = $now;
}

if ($_SESSION['rate_count'] >= $rateLimit) {
    http_response_code(429);
    header('Content-Type: application/json');
    echo json_encode(['error' => 'Rate limit exceeded. Try again later.']);
    exit;
}

$_SESSION['rate_count']++;
$threatScore = $_SESSION['threat_score'] ?? 0;
session_write_close();

// --- Request Validation ---
if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
    http_response_code(405);
    header('Content-Type: application/json');
    echo json_encode(['error' => 'Method not allowed']);
    exit;
}

$body = file_get_contents('php://input');
$payload = json_decode($body, true);

if (!$payload || empty($payload['message']) || !is_string($payload['message'])) {
    http_response_code(400);
    header('Content-Type: application/json');
    echo json_encode(['error' => 'Missing or invalid "message" field']);
    exit;
}

if (mb_strlen($payload['message']) > 1000) {
    http_response_code(400);
    header('Content-Type: application/json');
    echo json_encode(['error' => 'Message too long (max 1000 characters)']);
    exit;
}

// --- Proxy to Backend (streaming, with canary detection) ---
$canaryTripped = false;
$outputBuffer  = '';

$ch = curl_init($backendUrl . '/api/chat');
curl_setopt_array($ch, [
    CURLOPT_POST           => true,
    CURLOPT_POSTFIELDS     => json_encode(['message' => $payload['message']]),
    CURLOPT_HTTPHEADER     => [
        'Content-Type: application/json',
        'X-Api-Key: ' . $apiKey,
        'X-Threat-Score: ' . $threatScore,
    ],
    CURLOPT_HEADERFUNCTION => function($ch, $header) {
        if (stripos($header, 'X-Threat-Score:') === 0) {
            $score = (int) trim(substr($header, 15));
            session_start();
            $_SESSION['threat_score'] = ($_SESSION['threat_score'] ?? 0) + $score;
            session_write_close();
        }
        return strlen($header);
    },
    CURLOPT_WRITEFUNCTION  => function($ch, $data) use ($canary, &$canaryTripped, &$outputBuffer) {
        if ($canaryTripped) {
            return 0; // abort transfer
        }

        if ($canary !== '' && str_contains($data, $canary)) {
            $canaryTripped = true;
            return 0; // abort transfer
        }

        // Also check across chunk boundaries
        if ($canary !== '') {
            $outputBuffer .= $data;
            // Keep a sliding window slightly larger than the canary
            $keep = max(strlen($canary) * 2, 256);
            if (strlen($outputBuffer) > $keep) {
                $outputBuffer = substr($outputBuffer, -$keep);
            }
            if (str_contains($outputBuffer, $canary)) {
                $canaryTripped = true;
                return 0;
            }
        }

        echo $data;
        if (ob_get_level()) ob_flush();
        flush();
        return strlen($data);
    },
    CURLOPT_TIMEOUT        => 60,
    CURLOPT_CONNECTTIMEOUT => 5,
]);

header('Content-Type: text/event-stream');
header('Cache-Control: no-cache');
header('X-Accel-Buffering: no');

$success = curl_exec($ch);
$httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
curl_close($ch);

if ($canaryTripped) {
    // Terminate the SSE stream with an error and flag the session
    echo "data: \n\ndata: I can only discuss Bryan's professional experience.\n\ndata: [DONE]\n\n";
    if (ob_get_level()) ob_flush();
    flush();

    // Burn the session's remaining rate limit and spike threat score
    session_start();
    $_SESSION['rate_count'] = $rateLimit;
    $_SESSION['threat_score'] = ($_SESSION['threat_score'] ?? 0) + 50;
    session_write_close();
} elseif (!$success || $httpCode >= 400) {
    if (!headers_sent()) {
        http_response_code(502);
        header('Content-Type: application/json');
        echo json_encode(['error' => 'Chat backend unavailable']);
    }
}
