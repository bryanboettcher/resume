<?php
require __DIR__ . '/bootstrap.php';

// --- Session state (open once, close at end) ---
session_start();
checkRateLimit();
$threatScore = $_SESSION['threat_score'] ?? 0;
$history = $_SESSION['chat_history'] ?? [];

// --- Validate request ---
requirePost();
$payload = readJsonBody();
$userMessage = requireMessage($payload);

// --- Build backend payload ---
$backendPayload = ['message' => $userMessage];
if (!empty($history)) {
    $backendPayload['history'] = $history;
}

// --- Streaming callbacks ---
$canaryValue   = canary();
$canaryTripped = false;
$canaryBuffer  = '';
$rawResponse   = '';
$backendThreatScore = 0;

$onHeader = function($ch, $header) use (&$backendThreatScore) {
    if (stripos($header, 'X-Threat-Score:') === 0) {
        $backendThreatScore = (int) trim(substr($header, 15));
    }
    return strlen($header);
};

$onData = function($ch, $data) use ($canaryValue, &$canaryTripped, &$canaryBuffer, &$rawResponse) {
    if ($canaryTripped) return 0;

    $rawResponse .= $data;

    if ($canaryValue !== '') {
        $canaryBuffer .= $data;
        $keep = max(strlen($canaryValue) * 2, 256);
        if (strlen($canaryBuffer) > $keep) {
            $canaryBuffer = substr($canaryBuffer, -$keep);
        }
        if (str_contains($canaryBuffer, $canaryValue)) {
            $canaryTripped = true;
            return 0;
        }
    }

    echo $data;
    if (ob_get_level()) ob_flush();
    flush();
    return strlen($data);
};

// --- Execute streaming proxy ---
$ch = createBackendRequest('/api/chat', $backendPayload, [
    'X-Threat-Score: ' . $threatScore,
]);
curl_setopt($ch, CURLOPT_HEADERFUNCTION, $onHeader);
curl_setopt($ch, CURLOPT_WRITEFUNCTION, $onData);

header('Content-Type: text/event-stream');
header('Cache-Control: no-cache');
header('X-Accel-Buffering: no');

$success = curl_exec($ch);
$httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
curl_close($ch);

// --- Handle result ---
if ($canaryTripped) {
    echo "data: \n\ndata: I can only discuss Bryan's professional experience.\n\ndata: [DONE]\n\n";
    if (ob_get_level()) ob_flush();
    flush();
    burnRateLimit();
} elseif (!$success || $httpCode >= 400) {
    session_write_close();
    if (!headers_sent()) abort(502, 'Chat backend unavailable');
} else {
    $responseText = parseSseResponse($rawResponse);
    if ($responseText !== '') {
        pushHistory($userMessage, $responseText);
    }
    if ($backendThreatScore > 0) {
        $_SESSION['threat_score'] = $threatScore + $backendThreatScore;
    }
}

session_write_close();
