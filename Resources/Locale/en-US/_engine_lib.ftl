# Used internally by the THE() function.
zzzz-the = { PROPER($ent) ->
    *[false] the { $ent }
     [true] { $ent }
    }

# Used internally by the SUBJECT() function.
zzzz-subject-pronoun = { GENDER($ent) ->
    [male] he
    [female] she
    [epicene] they
   *[neuter] it
   }

# Used internally by the OBJECT() function.
zzzz-object-pronoun = { GENDER($ent) ->
    [male] him
    [female] her
    [epicene] them
   *[neuter] it
   }

# Used internally by the DAT-OBJ() function.
# Not used in en-US. Created for supporting other languages.
zzzz-dat-object = { GENDER($ent) ->
    [male] him
    [female] her
    [epicene] them
   *[neuter] it
   }

# Used internally by the POSS-PRONOUN() function.
zzzz-possessive-pronoun = { GENDER($ent) ->
    [male] his
    [female] hers
    [epicene] theirs
   *[neuter] its
   }

# Used internally by the POSS-ADJ() function.
zzzz-possessive-adjective = { GENDER($ent) ->
    [male] his
    [female] her
    [epicene] their
   *[neuter] its
   }

# Used internally by the REFLEXIVE() function.
zzzz-reflexive-pronoun = { GENDER($ent) ->
    [male] himself
    [female] herself
    [epicene] themselves
   *[neuter] itself
   }

# Used internally by the CONJUGATE-BE() function.
zzzz-conjugate-be = { GENDER($ent) ->
    [epicene] are
   *[other] is
   }

# Used internally by the CONJUGATE-HAVE() function.
zzzz-conjugate-have = { GENDER($ent) ->
    [epicene] have
   *[other] has
   }

# Used internally by the CONJUGATE-BASIC() function.
zzzz-conjugate-basic = { GENDER($ent) ->
    [epicene] { $first }
   *[other] { $second }
   }
