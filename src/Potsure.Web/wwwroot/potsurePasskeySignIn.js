window.potsurePasskeySignIn = {
  isAvailable() {
    return typeof PublicKeyCredential !== "undefined";
  },

  async signInWithPasskey(email, requestVerificationToken) {
    if (!this.isAvailable()) {
      return { ok: false, message: "Passkey verification is not available in this browser." };
    }

    const headers = {
      "Content-Type": "application/json",
      RequestVerificationToken: requestVerificationToken,
    };

    const optionsResponse = await fetch("/identity/passkeys/requestOptions", {
      method: "POST",
      credentials: "include",
      headers,
      body: JSON.stringify({ email: email || null }),
    });

    if (!optionsResponse.ok) {
      return { ok: false, message: "Could not start passkey sign-in." };
    }

    const optionsJson = await optionsResponse.json();
    const credential = await navigator.credentials.get({
      publicKey: PublicKeyCredential.parseRequestOptionsFromJSON(optionsJson),
    });

    if (!credential) {
      return { ok: false, message: "Passkey sign-in was cancelled." };
    }

    const loginResponse = await fetch("/identity/passkeys/login", {
      method: "POST",
      credentials: "include",
      headers,
      body: JSON.stringify({ credentialJson: JSON.stringify(credential) }),
    });

    if (!loginResponse.ok) {
      return { ok: false, message: "Passkey sign-in failed. Register a passkey if this is your first visit." };
    }

    return { ok: true };
  },

  async registerPasskey(email, requestVerificationToken) {
    if (!this.isAvailable()) {
      return { ok: false, message: "Passkey verification is not available in this browser." };
    }

    if (!email) {
      return { ok: false, message: "Enter your email to register a passkey." };
    }

    const headers = {
      "Content-Type": "application/json",
      RequestVerificationToken: requestVerificationToken,
    };

    const optionsResponse = await fetch("/identity/passkeys/register/options", {
      method: "POST",
      credentials: "include",
      headers,
      body: JSON.stringify({ email }),
    });

    if (!optionsResponse.ok) {
      const detail = await optionsResponse.text();
      return { ok: false, message: detail || "Could not start passkey registration." };
    }

    const optionsJson = await optionsResponse.json();
    const credential = await navigator.credentials.create({
      publicKey: PublicKeyCredential.parseCreationOptionsFromJSON(optionsJson),
    });

    if (!credential) {
      return { ok: false, message: "Passkey registration was cancelled." };
    }

    const registerResponse = await fetch("/identity/passkeys/register", {
      method: "POST",
      credentials: "include",
      headers,
      body: JSON.stringify({ credentialJson: JSON.stringify(credential) }),
    });

    if (!registerResponse.ok) {
      return { ok: false, message: "Passkey registration failed." };
    }

    return { ok: true };
  },
};
