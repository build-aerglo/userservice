/**
 * Frontend Password Encryption Utility
 *
 * This TypeScript utility encrypts passwords using AES-256-CBC encryption
 * that is compatible with the .NET EncryptionService.
 *
 * IMPORTANT: The encryption key must match the key in your .NET appsettings.json
 * Store this key securely (environment variable, secure config, etc.)
 *
 * Installation:
 * npm install crypto-js @types/crypto-js
 *
 * Usage:
 * import { encryptPassword } from './encryption';
 * const encryptedPassword = encryptPassword('MySecurePassword123!');
 */

import CryptoJS from 'crypto-js';

// This key MUST match the key in appsettings.json -> Encryption:Key
// In production, load this from environment variables
const ENCRYPTION_KEY = 'YourSecure32CharacterKeyHere123';

/**
 * Encrypts a password using AES-256-CBC encryption.
 * The IV is randomly generated and prepended to the ciphertext.
 * Output format: Base64(IV + Ciphertext)
 *
 * @param password - The plain text password to encrypt
 * @returns Base64 encoded encrypted password (IV + Ciphertext)
 */
export function encryptPassword(password: string): string {
  // Ensure key is exactly 32 bytes for AES-256
  const key = CryptoJS.enc.Utf8.parse(ENCRYPTION_KEY.padEnd(32).substring(0, 32));

  // Generate random 16-byte IV
  const iv = CryptoJS.lib.WordArray.random(16);

  // Encrypt using AES-256-CBC with PKCS7 padding
  const encrypted = CryptoJS.AES.encrypt(password, key, {
    iv: iv,
    mode: CryptoJS.mode.CBC,
    padding: CryptoJS.pad.Pkcs7
  });

  // Combine IV and ciphertext
  const ivBytes = iv;
  const cipherBytes = encrypted.ciphertext;

  // Concatenate IV + Ciphertext
  const combined = CryptoJS.lib.WordArray.create()
    .concat(ivBytes)
    .concat(cipherBytes);

  // Return as Base64
  return CryptoJS.enc.Base64.stringify(combined);
}

/**
 * Alternative implementation using native Web Crypto API (for modern browsers)
 * This is more secure as it doesn't require external dependencies
 *
 * @param password - The plain text password to encrypt
 * @returns Promise<string> - Base64 encoded encrypted password (IV + Ciphertext)
 */
export async function encryptPasswordNative(password: string): Promise<string> {
  // Convert key to bytes (ensure 32 bytes for AES-256)
  const keyBytes = new TextEncoder().encode(ENCRYPTION_KEY.padEnd(32).substring(0, 32));

  // Generate random 16-byte IV
  const iv = crypto.getRandomValues(new Uint8Array(16));

  // Import key for AES-CBC
  const cryptoKey = await crypto.subtle.importKey(
    'raw',
    keyBytes,
    { name: 'AES-CBC' },
    false,
    ['encrypt']
  );

  // Encrypt
  const passwordBytes = new TextEncoder().encode(password);
  const encrypted = await crypto.subtle.encrypt(
    { name: 'AES-CBC', iv: iv },
    cryptoKey,
    passwordBytes
  );

  // Combine IV and ciphertext
  const combined = new Uint8Array(iv.length + encrypted.byteLength);
  combined.set(iv, 0);
  combined.set(new Uint8Array(encrypted), iv.length);

  // Convert to Base64
  return btoa(String.fromCharCode(...combined));
}

// ============================================================================
// USAGE EXAMPLES
// ============================================================================

/*
// Example 1: Using CryptoJS (recommended for compatibility)
import { encryptPassword } from './encryption';

const handlePasswordReset = async (newPassword: string) => {
  const encryptedPassword = encryptPassword(newPassword);

  const response = await fetch('/api/password/reset-password', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      id: 'user@example.com',
      password: encryptedPassword
    })
  });

  const result = await response.json();
  console.log(result.message);
};

// Example 2: Using native Web Crypto API
import { encryptPasswordNative } from './encryption';

const handlePasswordResetNative = async (newPassword: string) => {
  const encryptedPassword = await encryptPasswordNative(newPassword);

  // ... rest of the request
};
*/

// ============================================================================
// REACT HOOK EXAMPLE
// ============================================================================

/*
import { useState, useCallback } from 'react';
import { encryptPassword } from './encryption';

interface PasswordResetState {
  loading: boolean;
  error: string | null;
  success: boolean;
}

export const usePasswordReset = () => {
  const [state, setState] = useState<PasswordResetState>({
    loading: false,
    error: null,
    success: false
  });

  const resetPassword = useCallback(async (identifier: string, newPassword: string) => {
    setState({ loading: true, error: null, success: false });

    try {
      const encryptedPassword = encryptPassword(newPassword);

      const response = await fetch('/api/password/reset-password', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          id: identifier,
          password: encryptedPassword
        })
      });

      if (!response.ok) {
        const error = await response.json();
        throw new Error(error.message || 'Password reset failed');
      }

      setState({ loading: false, error: null, success: true });
    } catch (err) {
      setState({
        loading: false,
        error: err instanceof Error ? err.message : 'Unknown error',
        success: false
      });
    }
  }, []);

  return { ...state, resetPassword };
};
*/

export default {
  encryptPassword,
  encryptPasswordNative
};
