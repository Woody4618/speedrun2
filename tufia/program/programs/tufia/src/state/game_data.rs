use anchor_lang::prelude::*;

use crate::{constants::MAX_WOOD_PER_TREE, GameErrorCode};

use super::player_data::PlayerData;

const BOARD_SIZE_X: usize = 10;
const BOARD_SIZE_Y: usize = 10;

const STATE_EMPTY: u8 = 0;
const STATE_PLAYER: u8 = 1;
const STATE_CHEST: u8 = 2;
const STATE_ENEMY: u8 = 3;

#[account(zero_copy(unsafe))]
#[repr(packed)]
#[derive(Default)]
pub struct GameData {
    pub data: [[TileData; BOARD_SIZE_X]; BOARD_SIZE_Y],
    pub total_wood_collected: u64,
}

#[zero_copy(unsafe)]
#[repr(packed)]
#[derive(Default)]
pub struct TileData {
    pub tile_type: u8,
    pub tile_level: u8,
    pub tile_owner: Pubkey, // Could maybe be the avatar of the player building it? :thinking:
    pub tile_xp: u64,
    pub tile_damage: u64,
    pub tile_defence: u64,
    pub tile_health: u64,
}

impl GameData {
    pub fn move_to_tile(
        &mut self,
        x: u64,
        y: u64,
        player: &mut PlayerData,
        spawn: bool,
    ) -> Result<()> {
        // Check if the player is on the board
        if x as usize >= BOARD_SIZE_X || y as usize >= BOARD_SIZE_Y {
            return Err(GameErrorCode::OutOfBounds.into());
        }

        let mut current_player_tile: Option<TileData> = None;
        let mut empty_slots: Vec<(usize, usize)> = Vec::new();

        for i in 0..BOARD_SIZE_X {
            for j in 0..BOARD_SIZE_Y {
                let tile: TileData = self.data[i][j];

                if tile.tile_owner == player.authority {
                    current_player_tile = Some(tile);
                    msg!("Found player tile")
                } else if tile.tile_type == STATE_EMPTY {
                    empty_slots.push((i as usize, j as usize));
                } else if tile.tile_owner == player.authority
                    && tile.tile_type == STATE_PLAYER
                    && spawn
                {
                    return Err(GameErrorCode::PlayerAlreadyExists.into());
                }
            }
        }

        match current_player_tile {
            Some(mut tile) => {
                self.data[x as usize][y as usize].tile_owner = tile.tile_owner;
                self.data[x as usize][y as usize].tile_health = tile.tile_health;
                self.data[x as usize][y as usize].tile_damage = tile.tile_damage;
                self.data[x as usize][y as usize].tile_defence = tile.tile_defence;
                self.data[x as usize][y as usize].tile_level = tile.tile_level;
                self.data[x as usize][y as usize].tile_type = STATE_PLAYER;
                self.data[x as usize][y as usize].tile_xp = tile.tile_xp;
                tile.tile_type = STATE_EMPTY;
            }
            None => {
                if spawn {
                    if empty_slots.len() == 0 {
                        return Err(GameErrorCode::BoardIsFull.into());
                    }

                    let mut rng = XorShift64 {
                        a: empty_slots.len() as u64,
                    };

                    let random_empty_slot =
                        empty_slots[(rng.next() % (empty_slots.len() as u64)) as usize];
                    msg!(
                        "Player spawn at {} {}",
                        random_empty_slot.0,
                        random_empty_slot.1
                    );

                    self.data[random_empty_slot.0][random_empty_slot.1] = TileData {
                        tile_owner: player.authority.clone(),
                        tile_type: STATE_PLAYER,
                        tile_health: player.health,
                        tile_damage: player.damage,
                        tile_defence: player.defence,
                        tile_level: player.level,
                        tile_xp: player.xp,
                        ..Default::default()
                    };

                    current_player_tile = Some(self.data[random_empty_slot.0][random_empty_slot.1]);
                } else {
                    return Err(GameErrorCode::PlayerNotOnBoard.into());
                }
            }
        }

        if current_player_tile.is_none() {
            return Err(GameErrorCode::PlayerNotOnBoard.into());
        }

        Ok(())
    }
}

pub struct XorShift64 {
    a: u64,
}

impl XorShift64 {
    pub fn next(&mut self) -> u64 {
        let mut x = self.a;
        x ^= x << 13;
        x ^= x >> 7;
        x ^= x << 17;
        self.a = x;
        x
    }
}
