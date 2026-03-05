import { useEffect, useState } from 'react'

function App() {
  const [message, setMessage] = useState<string | null>(null)

  useEffect(() => {
    fetch('/api/hello')
      .then((res) => res.json())
      .then((data) => setMessage(data.message))
      .catch(() => setMessage('Failed to fetch from backend'))
  }, [])

  return <h1>{message ?? 'Loading...'}</h1>
}

export default App
