<?php
/**
 * Test NotificationHub PHP SDK.
 * Usage: NH_API_KEY=nhub_live_xxx php tests/test-php.php
 */

$apiKey = getenv('NH_API_KEY');
$baseUrl = getenv('NH_BASE_URL') ?: 'https://api.notificationhub.space';
$email = getenv('TEST_EMAIL') ?: 'test@notificationhub.space';

if (!$apiKey) {
    echo "Set NH_API_KEY env var\n";
    exit(1);
}

function api(string $method, string $path, ?array $body = null): array
{
    global $apiKey, $baseUrl;

    $url = $baseUrl . $path;
    $ch = curl_init();
    curl_setopt_array($ch, [
        CURLOPT_URL            => $url,
        CURLOPT_RETURNTRANSFER => true,
        CURLOPT_TIMEOUT        => 30,
        CURLOPT_HTTPHEADER     => [
            'X-Api-Key: ' . $apiKey,
            'Content-Type: application/json',
        ],
    ]);

    if ($method === 'POST') {
        curl_setopt($ch, CURLOPT_POST, true);
        if ($body !== null) {
            curl_setopt($ch, CURLOPT_POSTFIELDS, json_encode($body));
        }
    }

    $response = curl_exec($ch);
    $statusCode = curl_getinfo($ch, CURLINFO_HTTP_CODE);
    curl_close($ch);

    if ($statusCode >= 400) {
        echo "   ✗ API $statusCode: $response\n";
        exit(1);
    }

    return json_decode($response, true) ?: [];
}

echo "=== NotificationHub PHP SDK Test ===\n\n";

// 1. Send
echo "1) Sending notification...\n";
$result = api('POST', '/api/v1/notifications', [
    'recipientEmail' => $email,
    'type'           => 'transactional',
    'channel'        => 'email',
    'payload'        => [
        'subject' => 'Test from PHP SDK - ' . date('H:i:s'),
        'html'    => '<h1>Hello!</h1><p>This is a test from the PHP SDK.</p>',
    ],
]);
$publicId = $result['publicId'];
echo "   ✓ Sent! ID: $publicId\n";

// 2. Get detail
echo "\n2) Getting notification detail...\n";
sleep(2);
$detail = api('GET', "/api/v1/notifications/$publicId");
echo "   ✓ Status: {$detail['status']} | Provider: " . ($detail['provider'] ?? 'pending') . "\n";

// 3. List
echo "\n3) Listing recent notifications...\n";
$list = api('GET', '/api/v1/notifications?page=1&pageSize=3');
echo "   ✓ Total: {$list['totalCount']} notifications\n";
foreach (array_slice($list['items'], 0, 3) as $n) {
    $pid = substr($n['publicId'], 0, 12);
    echo "   - $pid... | {$n['status']} | {$n['recipientEmail']}\n";
}

echo "\n=== All tests passed ===\n";
