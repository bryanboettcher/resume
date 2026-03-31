<?php
// Copy to config.php and fill in values. config.php is gitignored.
return [
    'backend_url' => 'https://resume-chat.your-cluster.example.com',
    'api_key'     => 'generate-a-guid-here',
    'rate_limit'  => 10,      // max requests per window per IP
    'rate_window' => 60,      // window in seconds
    'rate_path'   => '/tmp/resume-chat-rate',  // writable directory for rate limit files
];
