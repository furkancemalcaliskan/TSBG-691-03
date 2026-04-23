use std::env;
use std::io::{Read, Write};
use std::net::TcpStream;

fn main() {
    let command = env::args().nth(1).unwrap_or_else(|| "help".to_string());

    let result = match command.as_str() {
        "login-jwt" => login_jwt(),
        "call-jwt" => call_jwt(),
        "call-apikey" => call_apikey(),
        _ => {
            print_help();
            Ok(())
        }
    };

    if let Err(error) = result {
        eprintln!("Error: {error}");
        std::process::exit(1);
    }
}

fn login_jwt() -> Result<(), String> {
    let body = r#"{"username":"test","password":"test123"}"#;
    let response = http_request(
        "POST",
        "/login-jwt",
        &[("Content-Type", "application/json")],
        body,
    )?;

    println!("{response}");

    if let Some(token) = extract_json_string(&response, "token") {
        println!();
        println!("Use with:");
        println!("JWT_TOKEN=\"{token}\" cargo run -- call-jwt");
    }

    Ok(())
}

fn call_jwt() -> Result<(), String> {
    let token = env::args()
        .nth(2)
        .or_else(|| env::var("JWT_TOKEN").ok())
        .ok_or("JWT token missing. Run login-jwt first or pass token as argument.")?;

    let auth = format!("Bearer {token}");
    let response = http_request("GET", "/protected-jwt", &[("Authorization", &auth)], "")?;
    println!("{response}");
    Ok(())
}

fn call_apikey() -> Result<(), String> {
    let api_key = env::var("API_KEY").unwrap_or_else(|_| "demo-api-key-123".to_string());
    let response = http_request("GET", "/protected-apikey", &[("x-api-key", &api_key)], "")?;
    println!("{response}");
    Ok(())
}

fn http_request(
    method: &str,
    path: &str,
    headers: &[(&str, &str)],
    body: &str,
) -> Result<String, String> {
    let base = env::var("API_BASE").unwrap_or_else(|_| "http://127.0.0.1:5000".to_string());
    let (host, port) = parse_http_base(&base)?;
    let mut stream = TcpStream::connect((&host[..], port)).map_err(|error| error.to_string())?;

    let mut request = format!(
        "{method} {path} HTTP/1.1\r\nHost: {host}:{port}\r\nConnection: close\r\nContent-Length: {}\r\n",
        body.len()
    );

    for (name, value) in headers {
        request.push_str(&format!("{name}: {value}\r\n"));
    }

    request.push_str("\r\n");
    request.push_str(body);

    stream
        .write_all(request.as_bytes())
        .map_err(|error| error.to_string())?;

    let mut raw_response = String::new();
    stream
        .read_to_string(&mut raw_response)
        .map_err(|error| error.to_string())?;

    let Some((headers, body)) = raw_response.split_once("\r\n\r\n") else {
        return Ok(raw_response);
    };

    if headers.to_ascii_lowercase().contains("transfer-encoding: chunked") {
        return decode_chunked_body(body);
    }

    Ok(body.to_string())
}

fn parse_http_base(base: &str) -> Result<(String, u16), String> {
    let without_scheme = base
        .strip_prefix("http://")
        .ok_or("Only http:// API_BASE is supported in this minimal demo.")?;

    let host_port = without_scheme.trim_end_matches('/');
    let (host, port_text) = host_port
        .split_once(':')
        .ok_or("API_BASE must include host and port, for example http://127.0.0.1:5000")?;
    let port = port_text.parse::<u16>().map_err(|error| error.to_string())?;

    Ok((host.to_string(), port))
}

fn extract_json_string(json: &str, key: &str) -> Option<String> {
    let marker = format!("\"{key}\":\"");
    let start = json.find(&marker)? + marker.len();
    let rest = &json[start..];
    let end = rest.find('"')?;
    Some(rest[..end].to_string())
}

fn decode_chunked_body(body: &str) -> Result<String, String> {
    let mut rest = body;
    let mut decoded = String::new();

    loop {
        let Some((size_text, after_size)) = rest.split_once("\r\n") else {
            return Err("Invalid chunked response".to_string());
        };

        let size_hex = size_text.split(';').next().unwrap_or(size_text).trim();
        let size = usize::from_str_radix(size_hex, 16).map_err(|error| error.to_string())?;

        if size == 0 {
            return Ok(decoded);
        }

        if after_size.len() < size + 2 {
            return Err("Invalid chunk size".to_string());
        }

        decoded.push_str(&after_size[..size]);
        rest = &after_size[size + 2..];
    }
}

fn print_help() {
    println!("Authentication Demo CLI");
    println!();
    println!("Commands:");
    println!("  login-jwt          Get a JWT token");
    println!("  call-jwt <token>   Call /protected-jwt");
    println!("  call-apikey        Call /protected-apikey with x-api-key");
}
