import CryptoJS from "crypto-js";

const secretKey = "FAST-CREDITAPP";

export function encrypt(data) {
  return CryptoJS.AES.encrypt(data, secretKey).toString();
}
export function decrypt(cipherText) {
  const bytes = CryptoJS.AES.decrypt(cipherText, secretKey);
  return bytes.toString(CryptoJS.enc.Utf8);
}
