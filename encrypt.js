// utils/encrypt.js
export async function getKey(password = 'fH8dJ93kfPq3oW8kdPxrXxvDLkQaZ9rU') {
    const enc = new TextEncoder();
    const keyMaterial = await crypto.subtle.importKey(
      'raw',
      enc.encode(password),
      'PBKDF2',
      false,
      ['deriveKey']
    );
  
    return crypto.subtle.deriveKey(
      {
        name: 'PBKDF2',
        salt: enc.encode('wz5HdK3YqMw4Zr8L'),
        iterations: 100000,
        hash: 'SHA-256'
      },
      keyMaterial,
      { name: 'AES-GCM', length: 256 },
      false,
      ['encrypt', 'decrypt']
    );
  }
  
  export async function encryptData(data, key) {
    const enc = new TextEncoder();
    const iv = crypto.getRandomValues(new Uint8Array(12));
    const encoded = enc.encode(JSON.stringify(data));
  
    const encrypted = await crypto.subtle.encrypt(
      { name: 'AES-GCM', iv },
      key,
      encoded
    );
  
    const combined = new Uint8Array(iv.byteLength + encrypted.byteLength);
    combined.set(iv, 0);
    combined.set(new Uint8Array(encrypted), iv.byteLength);
  
    return btoa(String.fromCharCode(...combined));
  }
  