import { useEffect, useState } from "react";

export function Score({ total = 0, position, players, username, isPlayer }) {
  const [uname, setUname] = useState();

  useEffect(() => {
    if (isPlayer) {
      return setUname(username);
    }
    console.log(players, username);
    const opponent = players.find((p) => p !== username);
    setUname(opponent);
  }, []);

  return (
    <div className={position}>
      <h2>{uname}</h2>
      <h2>{total}</h2>
    </div>
  );
}
