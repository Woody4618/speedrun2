use crate::constants::*;
use anchor_lang::prelude::*;

use super::game_data::{TileData, TileData2};

#[account]
pub struct PlayerData {
    pub authority: Pubkey,
    pub name: String,
    pub level: u32,
    pub xp: u32,
    pub health: u32,
    pub max_health: u32,
    pub damage: u32,
    pub defence: u32,
    pub swords: u32,
    pub shields: u32,
    pub energy: u32,
    pub last_login: i64,
    pub last_id: u16,
    pub current_floor: u16,
    pub tile_data: TileData2,
    //pub inventory: Vec<Item>,
}

/*pub struct Item {
    pub name: String,
    pub defence: u64,
    pub damage: u64,
}*/

impl PlayerData {
    pub fn print(&mut self) -> Result<()> {
        // Note that logging costs a lot of compute. So don't use it too much.
        msg!(
            "Authority: {} Wood: {} Energy: {}",
            self.authority,
            self.health,
            self.energy
        );
        Ok(())
    }

    pub fn add_xp(&mut self, amount: u32) {
        self.xp += amount;

        while self.xp >= self.xp_threshold() {
            self.xp -= self.xp_threshold();
            self.level += 1;
            self.max_health += 1;
            self.health = self.max_health;
            self.damage += 1;

            println!("Leveled up! Current level: {}", self.level);
        }
    }

    pub fn xp_threshold(&self) -> u32 {
        5 * self.level // Example: Each level requires 100 * level XP
    }

    pub fn update_energy(&mut self) -> Result<()> {
        // Get the current timestamp
        let current_timestamp = Clock::get()?.unix_timestamp;

        // Calculate the time passed since the last login
        let mut time_passed: i64 = current_timestamp - self.last_login;

        // Calculate the time spent refilling energy
        let mut time_spent = 0;

        while time_passed >= TIME_TO_REFILL_ENERGY && self.energy < MAX_ENERGY {
            self.energy += 1;
            time_passed -= TIME_TO_REFILL_ENERGY;
            time_spent += TIME_TO_REFILL_ENERGY;
        }

        if self.energy >= MAX_ENERGY {
            self.last_login = current_timestamp;
        } else {
            self.last_login += time_spent;
        }

        Ok(())
    }

    pub fn move_to_tile(&mut self, x: u64, y: u64, player: Pubkey) -> Result<()> {
        Ok(())
    }
}
