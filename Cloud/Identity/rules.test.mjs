import { readFile, writeFile } from 'node:fs/promises';
import { initializeTestEnvironment, assertFails, assertSucceeds } from '@firebase/rules-unit-testing';
import { doc, setDoc, updateDoc, getDoc, getDocs, collection, deleteDoc, setLogLevel } from 'firebase/firestore';

// A demo project and loopback host are mandatory: never contact a production database.
if (process.env.GCLOUD_PROJECT !== 'demo-treasurerunner' || process.env.FIRESTORE_EMULATOR_HOST !== '127.0.0.1:8187') {
  throw new Error('Use the local demo emulator through the test script.');
}
setLogLevel('silent');
const report = [];
const env = await initializeTestEnvironment({
  projectId: 'demo-treasurerunner',
  firestore: { host: '127.0.0.1', port: 8187, rules: await readFile(new URL('firestore.rules', import.meta.url), 'utf8') }
});
const me = env.authenticatedContext('player-a').firestore();
const other = env.authenticatedContext('player-b').firestore();
const guest = env.unauthenticatedContext().firestore();
const profile = doc(me, 'players/player-a');
async function check(label, operation) { await operation(); report.push('PASS: ' + label); }
try {
  await env.clearFirestore();
  await check('Owner creates nickname with only schema and nickname fields', () => assertSucceeds(setDoc(profile, { schema: 1, nickname: 'Schatzjäger' })));
  await check('Owner reads own nickname', () => assertSucceeds(getDoc(profile)));
  await check('Signed-in friend can get a known player nickname', () => assertSucceeds(getDoc(doc(other, 'players/player-a'))));
  await check('Guest cannot read player nickname', () => assertFails(getDoc(doc(guest, 'players/player-a'))));
  await check('Guest cannot create profile', () => assertFails(setDoc(doc(guest, 'players/guest'), { schema: 1, nickname: 'Gast' })));
  await check('Other player cannot change nickname', () => assertFails(setDoc(doc(other, 'players/player-a'), { schema: 1, nickname: 'Fremd' })));
  await check('Owner can change chosen nickname', () => assertSucceeds(updateDoc(profile, { nickname: 'Held_02' })));
  for (const field of ['email', 'displayName', 'photoUrl', 'googleToken', 'uid']) {
    await check('Profile rejects extra provider field ' + field, () => assertFails(setDoc(profile, { schema: 1, nickname: 'Held', [field]: 'fixture' })));
  }
  for (const nickname of ['', 'ab', 'a'.repeat(21), ' Held', 'Held ', 'a@example.test', '<Held>', '\u202eHeld', '---', 123, null, 'Held\nTest']) {
    await check('Reject invalid nickname fixture ' + JSON.stringify(nickname), () => assertFails(setDoc(profile, { schema: 1, nickname })));
  }
  await check('Reject missing schema', () => assertFails(setDoc(profile, { nickname: 'Held' })));
  await check('Reject unknown schema', () => assertFails(setDoc(profile, { schema: 2, nickname: 'Held' })));
  await check('Reject missing nickname', () => assertFails(setDoc(profile, { schema: 1 })));
  await check('No player directory listing', () => assertFails(getDocs(collection(me, 'players'))));
  await check('No unrelated collection writes', () => assertFails(setDoc(doc(me, 'private/data'), { nickname: 'Held' })));
  await check('No unrelated collection reads', () => assertFails(getDoc(doc(me, 'private/data'))));
  await check('Other player cannot delete nickname', () => assertFails(deleteDoc(doc(other, 'players/player-a'))));
  const saved = (await getDoc(profile)).data();
  if (saved.nickname !== 'Held_02' || Object.keys(saved).sort().join(',') !== 'nickname,schema') throw new Error('Unexpected profile fields');
  report.push('PASS: Stored document contains chosen nickname and schema only');
  await check('Owner can delete own nickname', () => assertSucceeds(deleteDoc(profile)));
  report.push('ALL FIRESTORE IDENTITY RULES CHECKS PASSED');
} catch (error) {
  report.push('FAIL: ' + error.message);
  process.exitCode = 1;
} finally {
  await env.cleanup();
  await writeFile(new URL('../../Logs/GoogleIdentity/RulesChecks.txt', import.meta.url), report.join('\n') + '\n');
  console.log(report.join('\n'));
}
