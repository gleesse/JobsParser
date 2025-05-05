# Notes
- [ ] Power bi raport based on parsed data
Link parser could use yield return to improve efficiency and save partial progress in case of error. 

General todo
- [ ] Auto apply pracuj.json workflow replace timeout with waitForTimeoutSeconds
- [x] Auto-retry mechanizm to stop consuming for Retry-After header value or default 3 hours when received 429 Too Many Requests
- [x] Detail parser service - fix concurrent errors
- [x] Auto apply - select from offer where is_applied = 0
- [x] Auto apply - test on edge cases
- [x] Add logging to file
- [x] Replace wait idle with load/domload
- [x] Remove the default task.delays
- [x] Make checking the existence of an item with locator.count to avoid automatic wait and minimum timeout

Add support for
- [ ] https://theprotocol.it/
- [ ] https://justjoin.it/
- [ ] https://nofluffjobs.com/
- [ ] https://www.linkedin.com/
- [x] https://www.pracuj.pl/
- [ ] https://www.glassdoor.com/
      
Automatic application service
- [ ] https://theprotocol.it/
- [ ] https://justjoin.it/
- [ ] https://nofluffjobs.com/
- [ ] https://www.linkedin.com/
- [ ] https://www.pracuj.pl/
- [ ] https://www.glassdoor.com/

Hosting
- [x] Host link parsing service
- [x] Host detail parsing service
- [ ] Host auto apply service
- [x] Setup RabbitMq on server
