// Test NotificationHub TypeScript SDK
// Usage: NH_API_KEY=nhub_live_xxx npx tsx tests/test-typescript.ts

const API_KEY = process.env.NH_API_KEY;
const BASE_URL = process.env.NH_BASE_URL || 'https://api.notificationhub.space';
const EMAIL = process.env.TEST_EMAIL || 'test@notificationhub.space';

if (!API_KEY) {
  console.error('Set NH_API_KEY env var');
  process.exit(1);
}

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
  console.log('=== NotificationHub TypeScript SDK Test ===\n');

  // 1. Send
  console.log('1) Sending notification...');
  const sendResult = await api<{ publicId: string }>('POST', '/api/v1/notifications', {
    recipientEmail: EMAIL,
    type: 'transactional',
    channel: 'email',
    payload: JSON.stringify({
      subject: `Test from TypeScript SDK - ${new Date().toLocaleTimeString()}`,
      html: '<h1>Hello!</h1><p>This is a test from the TypeScript SDK.</p>',
    }),
  });
  console.log(`   ✓ Sent! ID: ${sendResult.publicId}`);

  // 2. Get detail
  console.log('\n2) Getting notification detail...');
  await new Promise(r => setTimeout(r, 2000));
  const detail = await api<any>('GET', `/api/v1/notifications/${sendResult.publicId}`);
  console.log(`   ✓ Status: ${detail.status} | Provider: ${detail.provider || 'pending'}`);

  // 3. List
  console.log('\n3) Listing recent notifications...');
  const list = await api<any>('GET', '/api/v1/notifications?page=1&pageSize=3');
  console.log(`   ✓ Total: ${list.totalCount} notifications`);
  list.items.slice(0, 3).forEach((n: any) => {
    console.log(`   - ${n.publicId.slice(0, 12)}... | ${n.status} | ${n.recipientEmail}`);
  });

  console.log('\n=== All tests passed ===');
}

main().catch(e => { console.error('✗ Failed:', e.message); process.exit(1); });
