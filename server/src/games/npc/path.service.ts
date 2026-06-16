import { Injectable } from '@nestjs/common';
import { MapData, Vec3 } from '../game.types';

@Injectable()
export class PathService {
  // ── 경로 탐색 ─────────────────────────────────────────────────────────────

  computePath(map: MapData, startWorld: Vec3, goalWorld: Vec3): Vec3[] {
    const toCell = (pos: Vec3) => ({
      row: Math.floor((pos.z - map.originZ) / map.cellSize),
      col: Math.floor((pos.x - map.originX) / map.cellSize),
    });

    const toWorld = (row: number, col: number): Vec3 => ({
      x: map.originX + (col + 0.5) * map.cellSize,
      y: 0,
      z: map.originZ + (row + 0.5) * map.cellSize,
    });

    const isWalkable = (row: number, col: number) =>
      row >= 0 &&
      row < map.height &&
      col >= 0 &&
      col < map.width &&
      map.grid[row][col] === 0;

    const start = toCell(startWorld);
    const goal = toCell(goalWorld);

    if (!isWalkable(start.row, start.col) || !isWalkable(goal.row, goal.col))
      return [];
    if (start.row === goal.row && start.col === goal.col)
      return [toWorld(goal.row, goal.col)];

    type Node = {
      row: number;
      col: number;
      g: number;
      f: number;
      parent: Node | null;
    };
    const key = (row: number, col: number) => `${row},${col}`;
    const h = (row: number, col: number) =>
      Math.abs(row - goal.row) + Math.abs(col - goal.col);

    const openMap = new Map<string, Node>();
    const closedSet = new Set<string>();
    openMap.set(key(start.row, start.col), {
      row: start.row,
      col: start.col,
      g: 0,
      f: h(start.row, start.col),
      parent: null,
    });

    const DIRS = [
      [-1, 0],
      [1, 0],
      [0, -1],
      [0, 1],
    ];

    while (openMap.size > 0) {
      let current: Node | null = null;
      for (const node of openMap.values()) {
        if (!current || node.f < current.f) current = node;
      }
      if (!current) break;

      openMap.delete(key(current.row, current.col));
      closedSet.add(key(current.row, current.col));

      if (current.row === goal.row && current.col === goal.col) {
        const path: Vec3[] = [];
        let node: Node | null = current;
        while (node) {
          path.unshift(toWorld(node.row, node.col));
          node = node.parent;
        }
        path.shift(); // 시작 셀 제거 (NPC는 이미 거기 있음)
        return path;
      }

      for (const [dr, dc] of DIRS) {
        const nr = current.row + dr;
        const nc = current.col + dc;
        const nk = key(nr, nc);
        if (!isWalkable(nr, nc) || closedSet.has(nk)) continue;

        const g = current.g + 1;
        const existing = openMap.get(nk);
        if (!existing || g < existing.g) {
          openMap.set(nk, {
            row: nr,
            col: nc,
            g,
            f: g + h(nr, nc),
            parent: current,
          });
        }
      }
    }

    return []; // 경로 없음
  }

  // ── 경로 후처리 ───────────────────────────────────────────────────────────

  smoothPath(map: MapData, start: Vec3, path: Vec3[]): Vec3[] {
    if (path.length <= 1) return [...path];

    const result: Vec3[] = [];
    let anchor = start;
    let i = 0;

    while (i < path.length) {
      let furthest = i;
      for (let j = i + 1; j < path.length; j++) {
        if (this.hasLineOfSight(map, anchor, path[j])) furthest = j;
      }
      result.push(path[furthest]);
      anchor = path[furthest];
      i = furthest + 1;
    }

    return result;
  }

  private hasLineOfSight(map: MapData, from: Vec3, to: Vec3): boolean {
    let row = Math.floor((from.z - map.originZ) / map.cellSize);
    let col = Math.floor((from.x - map.originX) / map.cellSize);
    const r1 = Math.floor((to.z - map.originZ) / map.cellSize);
    const c1 = Math.floor((to.x - map.originX) / map.cellSize);

    const dr = Math.abs(r1 - row);
    const dc = Math.abs(c1 - col);
    const sr = row < r1 ? 1 : -1;
    const sc = col < c1 ? 1 : -1;
    let err = dc - dr;

    while (true) {
      if (row < 0 || row >= map.height || col < 0 || col >= map.width)
        return false;
      if (map.grid[row][col] === 1) return false;
      if (row === r1 && col === c1) return true;

      const e2 = 2 * err;
      if (e2 > -dr) {
        err -= dr;
        col += sc;
      }
      if (e2 < dc) {
        err += dc;
        row += sr;
      }
    }
  }
}
