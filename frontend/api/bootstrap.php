<?php
declare(strict_types=1);

// --- Load & validate config ---
$configPath = __DIR__ . '/config.php';
if (!file_exists($configPath)) {
    abort(503, 'Chat not configured');
}

$config = require $configPath;

if (empty($config['backend_url']) || empty($config['api_key'])) {
    abort(503, 'Chat not configured');
}

// --- Helpers ---

function abort(int $status, string $message): never {
    http_response_code($status);
    header('Content-Type: application/json');
    echo json_encode(['error' => $message]);
    exit;
}

function backendUrl(string $path): string {
    global $config;
    return rtrim($config['backend_url'], '/') . $path;
}

function apiKey(): string {
    global $config;
    return $config['api_key'];
}

function canary(): string {
    global $config;
    return $config['canary'] ?? '';
}

function rateLimitConfig(): array {
    global $config;
    return [
        'limit'  => $config['rate_limit'] ?? 10,
        'window' => $config['rate_window'] ?? 60,
    ];
}

function requirePost(): void {
    if ($_SERVER['REQUEST_METHOD'] !== 'POST') {
        abort(405, 'Method not allowed');
    }
}

function readJsonBody(): array {
    $body = file_get_contents('php://input');
    $payload = json_decode($body, true);
    if (!$payload) {
        abort(400, 'Invalid JSON');
    }
    return $payload;
}

function requireMessage(array $payload): string {
    if (empty($payload['message']) || !is_string($payload['message'])) {
        abort(400, 'Missing or invalid "message" field');
    }
    if (mb_strlen($payload['message']) > 1000) {
        abort(400, 'Message too long (max 1000 characters)');
    }
    return $payload['message'];
}

function checkRateLimit(): void {
    $rl = rateLimitConfig();
    $now = time();

    if (!isset($_SESSION['rate_window_start']) || $now - $_SESSION['rate_window_start'] >= $rl['window']) {
        $_SESSION['rate_count'] = 0;
        $_SESSION['rate_window_start'] = $now;
    }

    if ($_SESSION['rate_count'] >= $rl['limit']) {
        abort(429, 'Rate limit exceeded. Try again later.');
    }

    $_SESSION['rate_count']++;
}

function parseSseResponse(string $raw): string {
    $text = '';
    foreach (explode("\n", $raw) as $line) {
        $line = trim($line);
        if (str_starts_with($line, 'data: ') && $line !== 'data: [DONE]') {
            $text .= substr($line, 6);
        }
    }
    return $text;
}

function pushHistory(string $prompt, string $response): void {
    session_start();
    $history = $_SESSION['chat_history'] ?? [];
    $history[] = ['prompt' => $prompt, 'response' => $response];
    while (count($history) > 6) {
        array_shift($history);
    }
    $_SESSION['chat_history'] = $history;
    session_write_close();
}

function burnRateLimit(): void {
    $rl = rateLimitConfig();
    session_start();
    $_SESSION['rate_count'] = $rl['limit'];
    $_SESSION['threat_score'] = ($_SESSION['threat_score'] ?? 0) + 50;
    session_write_close();
}

function createBackendRequest(string $path, array|null $payload = null, array $extraHeaders = [], string $method = 'POST', int $timeout = 60): CurlHandle {
    $ch = curl_init(backendUrl($path));

    $headers = ['Content-Type: application/json', 'X-Api-Key: ' . apiKey()];
    $headers = array_merge($headers, $extraHeaders);

    $opts = [
        CURLOPT_HTTPHEADER     => $headers,
        CURLOPT_TIMEOUT        => $timeout,
        CURLOPT_CONNECTTIMEOUT => min($timeout, 5),
    ];

    if ($method === 'GET') {
        $opts[CURLOPT_RETURNTRANSFER] = true;
    } else {
        $opts[CURLOPT_POST] = true;
        $opts[CURLOPT_POSTFIELDS] = json_encode($payload ?? []);
    }

    curl_setopt_array($ch, $opts);
    return $ch;
}
