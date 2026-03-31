<?php
declare(strict_types=1);

// --- Configuration ---
$config = require __DIR__ . '/config.php';
$backendUrl = $config['backend_url'];    // e.g. https://resume-chat.k8s.example.com
$apiKey     = $config['api_key'];        // shared GUID
$rateLimit  = $config['rate_limit'];     // requests per window
$rateWindow = $config['rate_window'];    // window in seconds
$ratePath   = $config['rate_path'];      // writable dir for rate files

// --- Rate Limiting (IP-based, file-backed) ---
$ip = $_SERVER['REMOTE_ADDR'] ?? 'unknown';
$rateFile = $ratePath . '/' . md5($ip) . '.json';

$now = time();
$entry = ['count' => 0, 'window_start' => $now];

if (file_exists($rateFile)) {
    $entry = json_decode(file_get_contents($rateFile), true) ?: $entry;
    if ($now - $entry['window_start'] >= $rateWindow) {
        $entry = ['count' => 0, 'window_start' => $now];
    }
}

if ($entry['count'] >= $rateLimit) {
    http_response_code(429);
    header('Content-Type: application/json');
    echo json_encode(['error' => 'Rate limit exceeded. Try again later.']);
    exit;
}

$entry['count']++;
file_put_contents($rateFile, json_encode($entry), LOCK_EX);

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

// --- Proxy to Backend (streaming) ---
$ch = curl_init($backendUrl . '/api/chat');
curl_setopt_array($ch, [
    CURLOPT_POST           => true,
    CURLOPT_POSTFIELDS     => json_encode(['message' => $payload['message']]),
    CURLOPT_HTTPHEADER     => [
        'Content-Type: application/json',
        'X-Api-Key: ' . $apiKey,
    ],
    CURLOPT_WRITEFUNCTION  => function($ch, $data) {
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
$curlError = curl_error($ch);
curl_close($ch);

if (!$success || $httpCode >= 400) {
    // If we haven't sent headers yet (unlikely with streaming, but safe)
    if (!headers_sent()) {
        http_response_code(502);
        header('Content-Type: application/json');
        echo json_encode(['error' => 'Chat backend unavailable']);
    }
}
