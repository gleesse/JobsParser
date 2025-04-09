# Todo List
- [ ] Describe patterns used. What can be done better, pros/cons of such approach

General todo
- [x] Mechanizm to stop consuming for 3 hours when received 429 Too Many Requests
- [ ] Add browser reuse instead of re-creation for each link
- [ ] Link parser options add flag to cut off query(for example, pracuj includes searchId query)
- [ ] Detail parser service - db context returned concurrent errors - to verify
- [ ] Auto apply - select from offer where is_applied = 0
- [ ] Auto apply - add cover letter generation and loading
- [ ] Auto apply - test on different cases
- [x] Adding an exit command. error/success
- [x] bool isAplied, shouldApply, appliedAt
- [x] Add error logging to file
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
