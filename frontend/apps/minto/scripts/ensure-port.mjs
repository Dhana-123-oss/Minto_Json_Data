import { execFileSync } from 'node:child_process';

const port = 5173;
if (process.platform === 'win32') {
  const output = execFileSync(
    'powershell.exe',
    ['-NoProfile', '-Command', `(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue).OwningProcess`],
    { encoding: 'utf8' }
  );
  const pids = [...new Set(
    output
      .split(/\r?\n/)
      .map(pid => pid.trim())
      .filter(pid => /^\d+$/.test(pid) && pid !== String(process.pid))
  )];
  for (const pid of pids) {
    if (pid !== String(process.pid)) {
      try { execFileSync('taskkill', ['/PID', pid, '/T', '/F'], { stdio: 'ignore' }); } catch { /* process already exited */ }
    }
  }
}
