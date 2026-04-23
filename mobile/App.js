import { useState } from "react";
import { Button, SafeAreaView, ScrollView, StyleSheet, Text, View } from "react-native";

const API_BASE = "http://10.0.2.2:5000";
const credentials = { username: "test", password: "test123" };

export default function App() {
  const [token, setToken] = useState("");
  const [result, setResult] = useState("Sonuc burada gorunecek.");

  async function loginJwt() {
    const response = await fetch(`${API_BASE}/login-jwt`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(credentials),
    });
    const data = await response.json();
    setToken(data.token ?? "");
    setResult(JSON.stringify(data, null, 2));
  }

  async function callProtected() {
    const response = await fetch(`${API_BASE}/protected-jwt`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    const text = await response.text();

    try {
      setResult(JSON.stringify(JSON.parse(text), null, 2));
    } catch {
      setResult(text);
    }
  }

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView contentContainerStyle={styles.page}>
        <Text style={styles.title}>Mobile JWT Demo</Text>
        <Text style={styles.text}>Token sadece React state icinde tutulur.</Text>

        <View style={styles.actions}>
          <Button title="JWT login" onPress={loginJwt} />
          <Button title="Protected cagir" onPress={callProtected} disabled={!token} />
        </View>

        <Text style={styles.label}>Token</Text>
        <Text selectable style={styles.box}>
          {token || "Token yok"}
        </Text>

        <Text style={styles.label}>Sonuc</Text>
        <Text selectable style={styles.box}>
          {result}
        </Text>
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: {
    flex: 1,
    backgroundColor: "#f5f7fb",
  },
  page: {
    padding: 20,
    gap: 14,
  },
  title: {
    fontSize: 28,
    fontWeight: "700",
    color: "#17202a",
  },
  text: {
    color: "#4f5b6b",
  },
  actions: {
    gap: 10,
  },
  label: {
    fontSize: 16,
    fontWeight: "700",
    color: "#17202a",
  },
  box: {
    borderWidth: 1,
    borderColor: "#d7dde8",
    borderRadius: 6,
    backgroundColor: "#ffffff",
    padding: 12,
    color: "#17202a",
  },
});
