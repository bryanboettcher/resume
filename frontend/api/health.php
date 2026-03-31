<?php
declare(strict_types=1);

$config = require __DIR__ . '/config.php';

$ch = curl_init($config['backend_url'] . '/api/chat/health');
curl_setopt_array($ch, [
    CURLOPT_RETURNTRANSFER => true,
    CURLOPT_TIMEOUT        => 3,
    CURLOPT_CONNECTTIMEOUT => 2,
]);

$response = curl_exec($ch);
$httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
curl_close($ch);

header('Content-Type: application/json');

if ($httpCode === 200) {
    echo $response;
} else {
    http_response_code(503);
    echo json_encode(['status' => 'unavailable']);
}
