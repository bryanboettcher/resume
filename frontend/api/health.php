<?php
require __DIR__ . '/bootstrap.php';

$ch = createBackendRequest('/api/chat/health', method: 'GET', timeout: 3);
$response = curl_exec($ch);
$httpCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
curl_close($ch);

header('Content-Type: application/json');
echo $httpCode === 200 ? $response : json_encode(['status' => 'unavailable']);
if ($httpCode !== 200) http_response_code(503);
