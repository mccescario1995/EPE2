import CryptoJS from "crypto-js";

const SECRET_KEY = "FAST-CREDITAPP";

export function encryptStorage(data: unknown): string {
  return CryptoJS.AES.encrypt(JSON.stringify(data), SECRET_KEY).toString();
}

export function decryptStorage<T = unknown>(cipherText: string): T | null {
  try {
    const bytes = CryptoJS.AES.decrypt(cipherText, SECRET_KEY);
    const decoded = bytes.toString(CryptoJS.enc.Utf8);
    if (!decoded) return null;
    return JSON.parse(decoded) as T;
  } catch {
    return null;
  }
}

export function encryptString(data: string): string {
  return CryptoJS.AES.encrypt(data, SECRET_KEY).toString();
}

export function decryptString(cipherText: string): string | null {
  try {
    const bytes = CryptoJS.AES.decrypt(cipherText, SECRET_KEY);
    const decoded = bytes.toString(CryptoJS.enc.Utf8);
    if (!decoded) return null;
    return decoded;
  } catch {
    return null;
  }
}
