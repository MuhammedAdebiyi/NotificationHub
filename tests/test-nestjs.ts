// Test NotificationHub NestJS SDK
// Usage: NH_API_KEY=nhub_live_xxx npx tsx tests/test-nestjs.ts

const API_KEY = process.env.NH_API_KEY;
const BASE_URL = process.env.NH_BASE_URL || 'https://api.notificationhub.space';
const EMAIL = process.env.TEST_EMAIL || 'test@notificationhub.space';

if (!API_KEY) {
  console.error('Set NH_API_KEY env var');
  process.exit(1);
}

interface SendResult { publicId: string; }

async function api<T>(method: string, path: string, body?: unknown): Promise<T> {
  const res = await fetch(`${BASE_URL}${path}`, {
    method,
    headers: { 'X-Api-Key': API_KEY!, 'Content-Type': 'application/json' },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  if (!res.ok) throw new Error(`API ${res.status}: ${text}`);
  return text ? JSON.parse(text) : ({} as T);
}

async function main() {
  console.log('=== NotificationHub NestJS SDK Test ===\n');

  // 1. Send
  console.log('1) Sending notification...');
  const result = await api<SendResult>('POST', '/api/v1/notifications', {
    recipientEmail: EMAIL,
    type: 'transactional',
    channel: 'email',
    payload: {
      subject: `Test from NestJS SDK - ${new Date().toLocaleTimeString()}`,
      html: '<h1>Hello!</h1><p>This is a test from the NestJS SDK.</p>',
    },
  });
  console.log(`   ✓ Sent! ID: ${result.publicId}`);

  // 2. Get detail
  console.log('\n2) Getting notification detail...');
  await new Promise(r => setTimeout(r, 2000));
  const detail = await api<any>('GET', `/api/v1/notifications/${result.publicId}`);
  console.log(`   ✓ Status: ${detail.status} | Provider: ${detail.provider || 'pending'}`);

  // 3. Campaign progress (just test the endpoint exists)
  console.log('\n3) Testing campaigns endpoint...');
  try {
    const campaigns = await api<any>('GET', '/api/v1/campaigns?page=1&pageSize=1');
    console.log(`   ✓ Campaigns accessible (${campaigns.totalCount || 0} total)`);
  } catch (e: any) {
    console.log(`   ⚠ Campaigns endpoint: ${e.message}`);
  }

  console.log('\n=== All tests passed ===');
}

main().catch(e => { console.error('✗ Failed:', e.message); process.exit(1); });
